// <auto-generated />
using AddressRegistry.Api.BackOffice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AddressRegistry.Api.BackOffice.Migrations
{
    [DbContext(typeof(BackOfficeContext))]
    [Migration("20220412085800_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("AddressRegistry.Api.BackOffice.AddressPersistentIdStreetNamePersistentId", b =>
                {
                    b.Property<int>("AddressPersistentLocalId")
                        .HasColumnType("int");

                    b.Property<int>("StreetNamePersistentLocalId")
                        .HasColumnType("int");

                    b.HasKey("AddressPersistentLocalId");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("AddressPersistentLocalId"));

                    b.ToTable("AddressPersistentIdStreetNamePersistentId", "AddressRegistryBackOffice");
                });
#pragma warning restore 612, 618
        }
    }
}
