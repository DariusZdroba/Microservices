using System;

namespace Play.Catalog.Service.Entities
{
    public interface IEntity
    {
        public Guid id { get; set; }
    }
}