using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class GameLiftManager : MonoBehaviour
{

    private const string m_targetFleet = "fleet-IDHERE";
    private string m_uniqueId = "Unique"; // TODO: This needs to be unique... Use username instead?

    private AmazonGameLiftClient M_gameLiftClient
    {
        get
        {
            if (_gameLiftClient == null)
            {
                var config = new AmazonGameLiftConfig();
                config.RegionEndpoint = RegionEndpoint.APSouth1;

                //IAM user credentials Which allows our client to make aws game lift calls like CreateGameSession. 
                //TODO: We can replace this with an individual users credentials
                string awsAccessKey = "KEYHERE";
                string awsSecretAccessKey = "KEYY/KEYYYYYYYYYYYYYY";
                _gameLiftClient = new AmazonGameLiftClient(awsAccessKey, awsSecretAccessKey, config);

            }
            return _gameLiftClient;
        }
        set
        {
            _gameLiftClient = value;
        }
    }
    private AmazonGameLiftClient _gameLiftClient = null;

    void Start()
    {
        UnityInitializer.AttachToGameObject(this.gameObject);
        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        //CreateGameSession();
        //FindGameSession();
    }

    /* public GameSession FindGameSession()
    {
        var threadStart = new ThreadStart(FindGameSessionGamelift);
        var myThread = new Thread(threadStart);
        myThread.Start();
        myThread.Join();//This seems dumb... but the function calling this one is ASync anyway.
        return GameSession
    }

    public void CreateGameSession()
    {
        var threadStart = new ThreadStart(CreateGameSessionGamelift);
        var myThread = new Thread(threadStart);
        myThread.Start();
        myThread.Join();//This seems dumb... but the function calling this one is ASync anyway.
    }

    public void JoinGameSession()
    {
        var threadStart = new ThreadStart(JoinGameSessionGamelift);
        var myThread = new Thread(threadStart);
        myThread.Start();
        myThread.Join();//This seems dumb... but the function calling this one is ASync anyway.
    }

    public void UpdateGameSession()
    {
        var threadStart = new ThreadStart(UpdateGameSessionGamelift);
        var myThread = new Thread(threadStart);
        myThread.Start();
        myThread.Join();//This seems dumb... but the function calling this one is ASync anyway.
    } */

    void Handler(Exception exception)
    {
        Debug.Log(exception);
    }

    public GameSession FindGameSession()
    {

        Debug.Log("FindGameSessionGameLift");

        //Search for active Game sessions:
        //https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/GameLift/TSearchGameSessionsRequest.html
        var request = new SearchGameSessionsRequest()
        {
            FilterExpression = "hasAvailablePlayerSessions=true",
            FleetId = m_targetFleet,
            Limit = 20,
            //FilterExpression = "maximumSessions>=10 AND hasAvailablePlayerSessions=true" // Example filter to limit search results
        };

        SearchGameSessionsResponse gameSessionlist = null;
        try
        {
            Debug.Log("Searching for game sessions");
            gameSessionlist = M_gameLiftClient.SearchGameSessions(request);
            Debug.Log("Done Searching for game sessions");
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        if (gameSessionlist == null)
        {
            Debug.Log("Unable to search for Game Sessions... What now?");
            return null;
        }

        int sessionsFound = gameSessionlist.GameSessions.Count;
        Debug.Log("Gamesessions found: " + sessionsFound);
        GameSession gameSession = null;
        for (int i = 0; i < sessionsFound; ++i)
        {
            //TODO: Implement logic here to determine best server.
            //Join first valid result returned 
            GameSession gameSessionCandidate = gameSessionlist.GameSessions[i];
            int currentPlayerCount = gameSessionCandidate.CurrentPlayerSessionCount;
            int maxPlayerCount = gameSessionCandidate.MaximumPlayerSessionCount;
            var creationPolicy = gameSessionCandidate.PlayerSessionCreationPolicy; // TODO: This is always null... How do we get the server to set this variable?

            if (currentPlayerCount < maxPlayerCount) // TODO: This if check will be useless once we implement the filter in the search game session request.
            {
                gameSession = gameSessionCandidate;
                break;
            }
        }
        if (gameSession == null)
        {
            Debug.Log("Unable to join any game sessions found. We should create one now...");
            return null;
            //TODO: Note: If we create a game session here and immediately try to join it, it may fail because the server status will not be set to "ACTIVE" yet. 
            //https://gamedev.amazon.com/forums/questions/61279/how-to-add-player-to-server-work-flow-using-gameli.html
        }
        Debug.Log("Game session successfully found");
        return gameSession;
    }
    public PlayerSession JoinGameSession(GameSession gameSession)
    {
        Debug.Log("Joining a game session");

        //TODO: Ensure the current Game Session is valid.

        Debug.Log("Creating player session");

        CreatePlayerSessionResponse playerSessionResponse = M_gameLiftClient.CreatePlayerSession(gameSession.GameSessionId, m_uniqueId);
        PlayerSession playerSession = playerSessionResponse.PlayerSession;

        Debug.Log("Done creating a player session");
        return playerSession;
    }

    public GameSession CreateGameSession()
    {

        Debug.Log("Creating a game session");

        var gameSessionRequest = new CreateGameSessionRequest()
        {
            FleetId = m_targetFleet,
            CreatorId = m_uniqueId,
            MaximumPlayerSessionCount = 3, //TODO: This needs to be 3, 6, or 9 depending on what mode they selected. We also need to add this information to the GameProperties list so the server knows to initialize itself as a 3, 6, or 9 player server
            Name = "GameTest",
            //GameProperties = //List of keyValue pairs for custom properties about our game. Ex: How many players it is??? 
        };

        GameSession gameSession = null;
        try
        {
            CreateGameSessionResponse createGameSessionResponse = M_gameLiftClient.CreateGameSession(gameSessionRequest);//TODO: It Fails here
            gameSession = createGameSessionResponse.GameSession;
            Debug.Log("Game session created. ID: " + gameSession.GameSessionId);
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        Debug.Log("Done Creating a gamesession");
        return gameSession;
    }

    public GameSession UpdateGameSession(GameSession gameSession)
    {
        if (gameSession == null)
        {
            return null;
        }

        UpdateGameSessionRequest updateGameSessionRequest = new UpdateGameSessionRequest()
        {
            GameSessionId = gameSession.GameSessionId,
        };

        var updateGameSessionResponse = M_gameLiftClient.UpdateGameSession(updateGameSessionRequest);
        return updateGameSessionResponse.GameSession;
    }
}


