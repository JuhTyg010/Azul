namespace Azul;

public class EventManager {
    private Queue<Action> pendingActions = new Queue<Action>();
    private bool isProcessingEvents = false;

    public void QueueEvent(Action action) {
        pendingActions.Enqueue(action);
    }

    public void ProcessPendingActions() {
        if (isProcessingEvents) return;

        isProcessingEvents = true;

        while (pendingActions.Count > 0) {
            var action = pendingActions.Dequeue();
            action();
        }

        isProcessingEvents = false;
    }
}