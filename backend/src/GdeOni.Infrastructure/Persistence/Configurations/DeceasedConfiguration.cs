using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GdeOni.Infrastructure.Persistence.Configurations;

public sealed class DeceasedConfiguration : IEntityTypeConfiguration<Deceased>
{
    
    public void Configure(EntityTypeBuilder<Deceased> builder)
    {
        builder.ToTable("deceased_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(Deceased.MaxShortDescriptionLength);

        builder.Property(x => x.Biography)
            .HasColumnName("biography")
            .HasMaxLength(Deceased.MaxBiographyLength);

        builder.Property(x => x.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(x => x.IsVerified)
            .HasColumnName("is_verified")
            .IsRequired();

        builder.Property(x => x.SearchKey)
            .HasColumnName("search_key")
            .HasMaxLength(Deceased.MaxSearchKey)
            .IsRequired();

        builder.HasIndex(x => x.SearchKey)
            .IsUnique()
            .HasDatabaseName(DbConstraints.DeceasedSearchKey);

        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(x => x.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(PersonName.MaxFirstName)
                .IsRequired();

            name.Property(x => x.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(PersonName.MaxLastName)
                .IsRequired();

            name.Property(x => x.MiddleName)
                .HasColumnName("middle_name")
                .HasMaxLength(PersonName.MaxMiddleName);
        });

        builder.OwnsOne(x => x.LifePeriod, lifePeriod =>
        {
            lifePeriod.Property(x => x.BirthDate)
                .HasColumnName("birth_date");

            lifePeriod.Property(x => x.DeathDate)
                .HasColumnName("death_date")
                .IsRequired();
        });

        builder.OwnsOne(x => x.BurialLocation, location =>
        {
            location.Property(x => x.Latitude)
                .HasColumnName("latitude")
                .IsRequired();

            location.Property(x => x.Longitude)
                .HasColumnName("longitude")
                .IsRequired();

            location.Property(x => x.AccuracyMeters)
                .HasColumnName("accuracy_meters");

            location.Property(x => x.Country)
                .HasColumnName("country")
                .HasMaxLength(BurialLocation.MaxCountryLength);

            location.Property(x => x.Region)
                .HasColumnName("region")
                .HasMaxLength(BurialLocation.MaxRegionLength);

            location.Property(x => x.City)
                .HasColumnName("city")
                .HasMaxLength(BurialLocation.MaxCityLength);

            location.Property(x => x.CemeteryName)
                .HasColumnName("cemetery_name")
                .HasMaxLength(BurialLocation.MaxCemeteryNameLength);

            location.Property(x => x.PlotNumber)
                .HasColumnName("plot_number")
                .HasMaxLength(BurialLocation.MaxPlotNumberLength);

            location.Property(x => x.GraveNumber)
                .HasColumnName("grave_number")
                .HasMaxLength(BurialLocation.MaxGraveNumberLength);

            location.Property(x => x.Accuracy)
                .HasColumnName("location_accuracy")
                .HasConversion<int>()
                .IsRequired();
        });

        builder.OwnsOne(x => x.Metadata, metadata =>
        {
            metadata.ToJson();

            metadata.Property(x => x.Epitaph)
                .HasMaxLength(DeceasedMetadata.MaxEpitaphLength);

            metadata.Property(x => x.Religion)
                .HasMaxLength(DeceasedMetadata.MaxReligionLength);

            metadata.Property(x => x.Source)
                .HasMaxLength(DeceasedMetadata.MaxSourceLength);

            metadata.Property(x => x.AdditionalInfo)
                .HasMaxLength(DeceasedMetadata.MaxAdditionalInfoLength);
        });

        builder.Navigation(x => x.Name).IsRequired();
        builder.Navigation(x => x.LifePeriod).IsRequired();
        builder.Navigation(x => x.Metadata).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Photos)
            .WithOne()
            .HasForeignKey("deceased_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Photos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Memories)
            .WithOne()
            .HasForeignKey("deceased_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Memories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Media)
            .WithOne()
            .HasForeignKey(x => x.DeceasedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Media)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.CreatedByUserId)
            .HasDatabaseName("ix_deceased_created_by_user_id");

        builder.HasIndex(x => x.IsVerified)
            .HasDatabaseName("ix_deceased_is_verified");

        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName("ix_deceased_created_at_utc");
    }
}