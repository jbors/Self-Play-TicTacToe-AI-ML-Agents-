using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

public enum HeuristicMethod
{
    Random,
    MinMax
}

public class TicTacToeAgent : Agent
{
    public Player type;
    [FormerlySerializedAs("boardState")] public Board board;
    public HeuristicMethod heuristicMethod;

    public override void OnEpisodeBegin()
    {
        type = GetComponent<BehaviorParameters>().TeamId == 0 ? Player.X : Player.O;
    }

    public override void Heuristic(float[] actionsOut)
    {
        var availableOptions = (int[]) board.GetAvailableFields();

        Debug.unityLogger.Log("Heuristic" + heuristicMethod);

        if (heuristicMethod == HeuristicMethod.Random)
        {
            int randomField = Random.Range(0, availableOptions.Length);
            Debug.unityLogger.Log("In random branch choice " + randomField);

            actionsOut[0] = randomField;
        }
        else if (heuristicMethod == HeuristicMethod.MinMax)
        {
            Debug.unityLogger.Log(string.Join(", ", availableOptions));
            //So which player to move?
            Debug.unityLogger.Log("Player " + type);

            //If the AI is the starting player, Start with a random move to keep it interesting (and not too slow)
            if (availableOptions.Length == 9)
            {
                actionsOut[0] = Random.Range(0, 9);
            }
            else
            {
                actionsOut[0] = BestMove(availableOptions, type);
            }
        }
    }
    
    private int BestMove(int[] availableOptions, Player player)
    {
        int[] results = EvaluateMoves(availableOptions, board.Fields, player);

        Debug.unityLogger.Log(string.Join(", ", results));
        
        // Return position with highest score
        int max = -2;
        int maxPos = -1;
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] > max)
            {
                max = results[i];
                maxPos = i;
            }
        }
        Debug.unityLogger.Log("Choose move at position " + maxPos);
        return availableOptions[maxPos];
    }

    private int BestMoveScore(Field[] state, int[] availableOptions, Player player)
    {
        int[] results = EvaluateMoves(availableOptions, state, player);
        
        // Return highest score
        int max = -2;
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] > max)
            {
                max = results[i];
            }
        }
        return max;
    }
    
    // Compute score for all available moves
    private int[] EvaluateMoves(int[] availableMoves, Field[] state, Player player)
    {
        var scores = new int[availableMoves.Length];

        for(int i = 0; i < availableMoves.Length; i++)
        {
            scores[i] = EvaluateMove(state, availableMoves[i], player);
        }

        return scores;
    }

    private int EvaluateMove(Field[] boardState, int move, Player player)
    {
        // First make a deep copy to not mess up game state;
        Field[] fields = boardState.Select(a =>
        {
            Field field = new Field();
            field.currentState = a.currentState;
            return field;
        }).ToArray();
        
        // Play the specified move
        if (player == Player.O)
        {
            fields[move].currentState = FieldState.Nought;
        }
        else
        {
            fields[move].currentState = FieldState.Cross;
        }

        //If game is finished, return score
        WinState state = BoardEvaluator.instance.Evaluate(fields);

        // Right now all games are scored as 0
        // TODO: return 1 or -1 depending on who won the game
        
        int[] availableOptions = GetAvailableFields(fields);
        
        //If nobody won and there are no more possible moves it is a draw
        if (availableOptions.Length == 0)
        {
            return 0;
        }
        
        // If game is not finished, go for the recursive step
        Player opposingPlayer = (player == Player.O) ? Player.X : Player.O;
        return -1 * BestMoveScore(fields, availableOptions, opposingPlayer);
    } 
    
    private int[] GetAvailableFields(Field[] state)
    {
        List<int> availableFields = new List<int>(9);

        for (int i = 0; i < state.Length; i++)
        {
            if (state[i].currentState == FieldState.None)
                availableFields.Add(i);
        }

        return availableFields.ToArray();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (var field in board.Fields)
        {
            sensor.AddOneHotObservation(field.ObserveField(type), 3);
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        board.SelectField(Mathf.FloorToInt(vectorAction[0]), type);
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        actionMasker.SetMask(0, board.GetOccupiedFields());
    }
}