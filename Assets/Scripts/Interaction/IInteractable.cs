namespace TheLostDungeon.Interaction
{
    /// <summary>
    /// Anything the player can use by looking at it and pressing Interact (E / gamepad Y):
    /// doors, switches, chests, terminals, NPCs...
    ///
    /// PlayerInteractor only knows this interface, so a new interactable type (e.g. an NPC that
    /// talks) works immediately without changing the player at all.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Text to show while looking at it ("Open door"), or null if it can't be used now.</summary>
        string GetPrompt(PlayerInteractor interactor);

        /// <summary>Performs the interaction. Returns false if nothing happened (e.g. door is locked).</summary>
        bool Interact(PlayerInteractor interactor);
    }

    /// <summary>
    /// Something another object can switch on and off: doors, lights, lifts, force fields.
    /// A WallSwitch drives any number of these without knowing what they are.
    /// </summary>
    public interface IActivatable
    {
        void SetActivated(bool activated);
    }
}
