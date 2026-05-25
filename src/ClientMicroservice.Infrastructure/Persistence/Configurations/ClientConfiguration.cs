using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClientMicroservice.Domain.Entities;

namespace ClientMicroservice.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.ProfilePictureUrl).HasColumnName("profile_picture_url");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("address_street").HasMaxLength(200).IsRequired();
            a.Property(x => x.City).HasColumnName("address_city").HasMaxLength(100).IsRequired();
            a.Property(x => x.State).HasColumnName("address_state").HasMaxLength(100).IsRequired();
            a.Property(x => x.ZipCode).HasColumnName("address_zip_code").HasMaxLength(20).IsRequired();
            a.Property(x => x.Country).HasColumnName("address_country").HasMaxLength(100).IsRequired();
        });

        builder.OwnsOne(c => c.BankingDetails, b =>
        {
            b.Property(x => x.Agency).HasColumnName("banking_agency").HasMaxLength(50).IsRequired();
            b.Property(x => x.AccountNumber).HasColumnName("banking_account_number").HasMaxLength(50).IsRequired();
        });
    }
}
