namespace Player
{
    public interface ISerializablePlayerData
    {
        void LoadPlayerData(PlayerData playerData);
        
        /// <summary>
        /// Save the progress actually.
        /// </summary>
        /// <param name="playerData"></param>
        void GetPlayerData(PlayerData playerData);
    }
}