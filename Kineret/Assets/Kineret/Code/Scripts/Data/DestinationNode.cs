using System;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    Up,Down, Left, Right
}

public class DestinationsGraph
{
    public DestinationNode CurrentDestinationNode;

    public DestinationsGraph(List<DestinationNodeData> destinationNodesData, Dictionary<string, DestinationButtonHandler> destinationButtonHandlers)
    {
        Dictionary<string, DestinationNode> nodes = new();
        string destiantionCodeName;

        Debug.Log(destinationNodesData == null);
        foreach (var destinationNodeData in destinationNodesData)
        {
            destiantionCodeName = destinationNodeData.DestinationCodeName;
            nodes.Add(destiantionCodeName, new DestinationNode(destinationButtonHandlers[destiantionCodeName]));
        }

        foreach (var destinationNodeData in destinationNodesData)
        {
            destiantionCodeName = destinationNodeData.DestinationCodeName;
            nodes[destiantionCodeName].UpNode = nodes[destinationNodeData.UpDestinationCodeName];
            nodes[destiantionCodeName].DownNode = nodes[destinationNodeData.DownDestinationCodeName];
            nodes[destiantionCodeName].LeftNode = nodes[destinationNodeData.LeftDestinationCodeName];
            nodes[destiantionCodeName].RightNode = nodes[destinationNodeData.RightDestinationCodeName];
        }

        CurrentDestinationNode = nodes[destinationNodesData[0].DestinationCodeName];
    }

    public void NextNode(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                CurrentDestinationNode = CurrentDestinationNode.UpNode;
                break;
            case Direction.Down:
                CurrentDestinationNode = CurrentDestinationNode.DownNode;
                break;
            case Direction.Left:
                CurrentDestinationNode = CurrentDestinationNode.LeftNode;
                break;
            case Direction.Right:
                CurrentDestinationNode = CurrentDestinationNode.RightNode;
                break;
        }

    }
}
[Serializable]
public struct GameDestinationNodeData
{
    public List<DestinationNodeData> DestinationNodesData;
}
[Serializable]
public struct DestinationNodeData
{
    public string DestinationCodeName;
    public string UpDestinationCodeName;
    public string DownDestinationCodeName;
    public string LeftDestinationCodeName;
    public string RightDestinationCodeName;
}

public class DestinationNode
{
    public DestinationButtonHandler DestinationButtonHandler;
    public DestinationNode UpNode;
    public DestinationNode DownNode;
    public DestinationNode LeftNode;
    public DestinationNode RightNode;

    public DestinationNode(DestinationButtonHandler destinationButtonHandler)
    {
        DestinationButtonHandler = destinationButtonHandler;
    }
}
