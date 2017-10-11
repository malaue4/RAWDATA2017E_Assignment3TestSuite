using System.Collections.Generic;

namespace TestServer
{
    public class DataBase
    {
        private List<Category> _categories;

        public DataBase()
        {
            _categories = new List<Category>
            {
                new Category
                {
                    Id = 1,
                    Name = "Beverages"
                },
                new Category
                {
                    Id = 2,
                    Name = "Condiments"
                },
                new Category
                {
                    Id = 3,
                    Name = "Confections"
                }
            };
        }

        public bool HasCategory(int id)
        {
            foreach (var category in _categories)
            {
                if (category.Id == id) return true;
            }
            return false;
        }

        public Category GetCategory(int id)
        {
            foreach (var category in _categories)
            {
                if (category.Id == id) return category;
            }
            return null;
        }

        public bool DeleteCategory(Category category)
        {
            return _categories.Remove(category);
        }

        public Category CreateCategory(string name)
        {
            var category = new Category
            {
                Id = LowestFreeId(),
                Name = name
            };
            _categories.Add(category);
            return category;
        }

        public void UpdateCategory(Category category)
        {
            GetCategory(category.Id).Name = category.Name;
        }

        private int LowestFreeId()
        {
            int id = 1;
            while (HasCategory(id))
            {
                id++;
            }
            return id;
        }

        public List<Category> GetCategories()
        {
            return _categories;
        }
    }
}