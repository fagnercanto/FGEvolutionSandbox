using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG.Evolution.Simulation.Engine.Graphics
{
    public enum FormatoVisual
    {
        QuadradoComNariz, // Específico para entidades com direção (ex: Minions)
        Quadrado,         // Elementos estáticos e genéricos (ex: Comida, Paredes)
        Circulo           // Para futuras implementações do Sandbox
    }
}
