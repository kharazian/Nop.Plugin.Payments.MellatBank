using Nop.Plugin.Payments.MellatBank.Domain;
using System.Data.Entity.ModelConfiguration;


namespace Nop.Plugin.Payments.MellatBank.DataMappings
{
    public class TransactionMap : EntityTypeConfiguration<Transaction>
    {
        public TransactionMap()
        {
            ToTable("Transaction_IR");

            HasKey(pt => pt.Id);
            Property(pt => pt.TransactionId).IsRequired();
        }
    }
}