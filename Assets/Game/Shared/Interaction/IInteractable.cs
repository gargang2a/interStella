namespace InterStella.Game.Shared.Interaction
{
    public interface IInteractable
    {
        bool TryInteract(in InteractionContext context);
    }
}
