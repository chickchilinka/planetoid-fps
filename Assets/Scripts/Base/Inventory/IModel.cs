namespace Inventory
{
    public interface IModel : IIdentified
    {
    }
    
    public interface IInventoryModel : IModel
    {
        void Clear();
    }
    
    public interface IInventorySerializableModel<TData> : IInventoryModel, ISerializable<TData>
    {
    }
}
