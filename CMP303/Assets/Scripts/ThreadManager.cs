using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    // Action list variables
    private static readonly List<Action> executeOnMainThread = new List<Action>();
    private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
    private static bool actionToExecuteOnMainThread = false;

    void Update()
    {
        UpdateMain();
    }

    public static void ExecuteOnMainThread(Action action)
    {
        // Checks if action is executable
        if (action == null)
        {
            Debug.Log("No action to execute on main thread!");
            return;
        }

        // Adds action to queue 
        lock (executeOnMainThread)
        {
            executeOnMainThread.Add(action);
            actionToExecuteOnMainThread = true;
        }
    }

    
    public static void UpdateMain()
    {
        // If action is available
        if (actionToExecuteOnMainThread)
        {
            // Make copies of main thread actions to execute and clear main thread queue
            executeCopiedOnMainThread.Clear();
            lock (executeOnMainThread)
            {
                executeCopiedOnMainThread.AddRange(executeOnMainThread);
                executeOnMainThread.Clear();
                actionToExecuteOnMainThread = false;
            }

            // Execute all actions queued
            for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
            {
                executeCopiedOnMainThread[i]();
            }
        }
    }
}
