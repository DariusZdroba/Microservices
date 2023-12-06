using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;
using Play.Catalog.Service;
using Play.Catalog.Contracts;

namespace Play.Catalog.Service.Controllers
{
    //https://localhost:5001/items
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> itemsRepository;
        private readonly IPublishEndpoint publishEndpoint;
        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
        {
            this.itemsRepository = itemsRepository;
            this.publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            var items = (await itemsRepository.GetAllAsync())
                        .Select(item => item.AsDto());
            return Ok(items);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);
            if (item == null) return NotFound();
            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                name = createItemDto.name,
                description = createItemDto.description,
                price = createItemDto.price,
                createdDate = DateTimeOffset.UtcNow
            };
            await itemsRepository.CreateAsync(item);

            await publishEndpoint.Publish(new CatalogItemCreated(item.id, item.name, item.description));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.id }, item);
        }

        //PUT /items/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await itemsRepository.GetAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }
            existingItem.name = updateItemDto.name;
            existingItem.description = updateItemDto.description;
            existingItem.price = updateItemDto.price;

            await itemsRepository.UpdateAsync(existingItem);

            await publishEndpoint.Publish(new CatalogItemUpdated(existingItem.id, existingItem.name, existingItem.description));


            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var existingItem = await itemsRepository.GetAsync(id);
            if (existingItem == null)
            {
                return NotFound();
            }
            await itemsRepository.RemoveAsync(existingItem.id);

            await publishEndpoint.Publish(new CatalogItemDeleted(existingItem.id));


            return NoContent();

        }
    }
}