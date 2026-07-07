using ClickMage.Interface;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CommandQueue<TCharacter> where TCharacter : BaseCharacter
{
    private TCharacter _owner;
    private ICommand<TCharacter> _current;
    private readonly Queue<ICommand<TCharacter>> _queue = new();

    public event Action OnQueueEmptied;

    public bool IsEmpty() => _current == null && _queue.Count == 0;

    public void Initialize(TCharacter owner)
    {
        _owner = owner;
    }

    public void GiveCommand(ICommand<TCharacter> command)
    {
        _current?.Cancel();
        _queue.Clear();
        _current = command;
        _current.Start(_owner);
    }

    public void QueueCommand(ICommand<TCharacter> command)
    {
        if (_current == null)
        {
            _current = command;
            _current.Start(_owner);
        }
        else
        {
            _queue.Enqueue(command);
        }
    }

    public void Clear()
    {
        _current?.Cancel();
        _current = null;
        _queue.Clear();
    }


    public void Tick(float deltaTime)
    {
        // Start next command if none running
        if (_current == null)
        {
            if (_queue.Count == 0) return;
            _current = _queue.Dequeue();
            _current.Start(_owner);
        }

        _current.Tick(_owner, deltaTime);

        if (_current.IsComplete)
        {
            _current = null;

            // Fire event when the entire queue drains
            if (_queue.Count == 0)
            {
                OnQueueEmptied?.Invoke();
            }
        }
    }
}
