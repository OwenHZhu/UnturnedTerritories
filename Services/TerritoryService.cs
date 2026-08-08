using System.Collections.Generic;
using TerritoryPlugin.Models;

namespace TerritoryPlugin.Services
{
    public class TerritoryService
    {
        private readonly List<Territory> m_Territories = new List<Territory>();

        public IReadOnlyList<Territory> Territories => m_Territories;

        public void AddTerritory(Territory territory)
        {
            m_Territories.Add(territory);
        }

        public Territory GetTerritoryAt(float x, float z)
        {
            foreach (var territory in m_Territories)
            {
                float dx = x - territory.X;
                float dz = z - territory.Z;

                float distanceSquared = (dx * dx) + (dz * dz);
                float radiusSquared = territory.Radius * territory.Radius;

                if (distanceSquared <= radiusSquared)
                {
                    return territory;
                }
            }

            return null;
        }
    }
}