using System;

namespace Muzu.Api.Core.Models
{
    public class TenantConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Moneda { get; set; } = "USD";
        public decimal LimiteConsumoFijo { get; set; } = 35;
        public decimal PrecioConsumoFijo { get; set; } = 3;
        public decimal LimiteConsumoExtra1 { get; set; } = 45;
        public decimal PorcentajeExtra1 { get; set; } = 0.15m;
        public decimal LimiteConsumoExtra2 { get; set; } = 55;
        public decimal PorcentajeExtra2 { get; set; } = 0.25m;
        public decimal MultaRetraso { get; set; } = 2;
        public decimal MultaNoAsistirReunion { get; set; } = 5;
        public decimal MultaNoAsistirTrabajo { get; set; } = 10;
    }
}
