using Microsoft.EntityFrameworkCore;
using LaborControl.API.Models;

namespace LaborControl.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables existantes
        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RfidChip> RfidChips { get; set; }
        public DbSet<ControlPoint> ControlPoints { get; set; }
        public DbSet<ScheduledTask> ScheduledTasks { get; set; }
        public DbSet<TaskExecution> TaskExecutions { get; set; }
        public DbSet<TaskTemplate> TaskTemplates { get; set; }
        public DbSet<TaskDeviation> TaskDeviations { get; set; }

        // NOUVELLES tables pour architecture multi-sites
        public DbSet<Site> Sites { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Team> Teams { get; set; }

        // Table Orders (commandes)
        public DbSet<Order> Orders { get; set; }

        // Tables pour le catalogue et le panier
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // Tables pour le système de qualifications personnalisables (multimétier)
        public DbSet<Qualification> Qualifications { get; set; }
        public DbSet<UserQualification> UserQualifications { get; set; }
        public DbSet<TaskTemplateQualification> TaskTemplateQualifications { get; set; }
        public DbSet<MaintenanceScheduleQualification> MaintenanceScheduleQualifications { get; set; }

        // Tables pour Secteurs et Métiers
        public DbSet<Sector> Sectors { get; set; }
        public DbSet<Industry> Industries { get; set; }

        // Tables de référence maître pour secteurs et métiers prédéfinis (gérés par app-staff)
        public DbSet<PredefinedSector> PredefinedSectors { get; set; }
        public DbSet<PredefinedIndustry> PredefinedIndustries { get; set; }
        public DbSet<PredefinedQualification> PredefinedQualifications { get; set; }
        public DbSet<PredefinedQualificationSector> PredefinedQualificationSectors { get; set; }

        // Tables pour paramètres équipements
        public DbSet<EquipmentCategory> EquipmentCategories { get; set; }
        public DbSet<EquipmentType> EquipmentTypes { get; set; }
        public DbSet<EquipmentStatus> EquipmentStatuses { get; set; }
        public DbSet<FavoriteManufacturer> FavoriteManufacturers { get; set; }

        // Tables de référence maître pour équipements prédéfinis (gérés par app-staff)
        public DbSet<PredefinedEquipmentCategory> PredefinedEquipmentCategories { get; set; }
        public DbSet<PredefinedEquipmentType> PredefinedEquipmentTypes { get; set; }

        // Tables pour Gammes de Maintenance
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public DbSet<MaintenanceTask> MaintenanceTasks { get; set; }
        public DbSet<MaintenanceExecution> MaintenanceExecutions { get; set; }

        // Table pour historique des changements de statut RFID
        public DbSet<RfidChipStatusHistory> RfidChipStatusHistory { get; set; }

        // Tables pour gestion des fournisseurs
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierOrder> SupplierOrders { get; set; }
        public DbSet<SupplierOrderLine> SupplierOrderLines { get; set; }

        // Table pour utilisateurs staff Labor Control (séparation totale des clients)
        public DbSet<StaffUser> StaffUsers { get; set; }

        // Table pour contenu éditable de la page d'accueil
        public DbSet<HomeContent> HomeContents { get; set; }

        // Table pour tokens de réinitialisation de mot de passe
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        // Table pour journal des erreurs applicatives
        public DbSet<ErrorLog> ErrorLogs { get; set; }

        // Table pour expéditions Boxtal
        public DbSet<BoxtalShipment> BoxtalShipments { get; set; }

        // ========== TABLES NUCLÉAIRES DMTT ==========

        /// <summary>
        /// Soudures nucléaires
        /// </summary>
        public DbSet<Weld> Welds { get; set; }

        /// <summary>
        /// Matériaux avec validation CCPU
        /// </summary>
        public DbSet<Material> Materials { get; set; }

        /// <summary>
        /// DMOS - Descriptif Mode Opératoire de Soudage
        /// </summary>
        public DbSet<DMOS> DMOSs { get; set; }

        /// <summary>
        /// Programmes de Contrôle Non Destructif
        /// </summary>
        public DbSet<NDTProgram> NDTPrograms { get; set; }

        /// <summary>
        /// Contrôles CND effectués
        /// </summary>
        public DbSet<NDTControl> NDTControls { get; set; }

        /// <summary>
        /// Fiches de Non-Conformité
        /// </summary>
        public DbSet<NonConformity> NonConformities { get; set; }

        /// <summary>
        /// Qualifications soudeurs/contrôleurs CND
        /// </summary>
        public DbSet<WelderQualification> WelderQualifications { get; set; }

        /// <summary>
        /// Documents techniques (CDC, Plans, Normes, Certificats)
        /// </summary>
        public DbSet<TechnicalDocument> TechnicalDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Customer
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            });

            // Site (NOUVEAU)
            modelBuilder.Entity<Site>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Sites)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Zone (NOUVEAU)
            modelBuilder.Entity<Zone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Site)
                    .WithMany(s => s.Zones)
                    .HasForeignKey(e => e.SiteId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ParentZone)
                    .WithMany(z => z.SubZones)
                    .HasForeignKey(e => e.ParentZoneId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Asset (NOUVEAU)
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.Zone)
                    .WithMany(z => z.Assets)
                    .HasForeignKey(e => e.ZoneId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ParentAsset)
                    .WithMany(a => a.SubAssets)
                    .HasForeignKey(e => e.ParentAssetId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Team (NOUVEAU)
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Teams)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Supervisor)
                    .WithMany()
                    .HasForeignKey(e => e.SupervisorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Sector)
                    .WithMany()
                    .HasForeignKey(e => e.SectorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                // Hiérarchie Service > Équipe
                entity.HasOne(e => e.ParentTeam)
                    .WithMany(t => t.SubTeams)
                    .HasForeignKey(e => e.ParentTeamId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.JobTitle).HasMaxLength(100);
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Users)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Team)
                    .WithMany(t => t.Members)
                    .HasForeignKey(e => e.TeamId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Site)
                    .WithMany(s => s.Users)
                    .HasForeignKey(e => e.SiteId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Supervisor)
                    .WithMany()
                    .HasForeignKey(e => e.SupervisorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Sector)
                    .WithMany()
                    .HasForeignKey(e => e.SectorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Industry)
                    .WithMany()
                    .HasForeignKey(e => e.IndustryId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // RfidChip
            modelBuilder.Entity<RfidChip>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ChipId).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.ChipId).IsUnique();
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.RfidChips)
                    .HasForeignKey(e => e.CustomerId);
            });

            // ControlPoint (MODIFIÉ pour lien avec Asset OPTIONNEL)
            modelBuilder.Entity<ControlPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId);
                entity.HasOne(e => e.Asset)
                    .WithMany(a => a.ControlPoints)
                    .HasForeignKey(e => e.AssetId)
                    .IsRequired(false)  // ⚠️ AJOUT IMPORTANT - Asset optionnel temporairement
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Zone)
                    .WithMany(z => z.ControlPoints)
                    .HasForeignKey(e => e.ZoneId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.RfidChip)
                    .WithMany()
                    .HasForeignKey(e => e.RfidChipId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // TaskTemplate
            modelBuilder.Entity<TaskTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.TaskTemplates)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaskDeviation
            modelBuilder.Entity<TaskDeviation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.TaskExecution)
                    .WithMany()
                    .HasForeignKey(e => e.TaskExecutionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PerformedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.PerformedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ScheduledTask
            modelBuilder.Entity<ScheduledTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.ControlPoint)
                    .WithMany(cp => cp.ScheduledTasks)
                    .HasForeignKey(e => e.ControlPointId);
                entity.HasOne(e => e.TaskTemplate)
                    .WithMany(t => t.ScheduledTasks)
                    .HasForeignKey(e => e.TaskTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaskExecution
            modelBuilder.Entity<TaskExecution>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.ScheduledTask)
                    .WithMany()
                    .HasForeignKey(e => e.ScheduledTaskId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ControlPoint)
                    .WithMany(cp => cp.TaskExecutions)
                    .HasForeignKey(e => e.ControlPointId);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId);
            });

            // Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DeliveryAddress).IsRequired().HasMaxLength(500);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ProductType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
            });

            // CartItem
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
            });

            // Qualification (système personnalisable multimétier)
            modelBuilder.Entity<Qualification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // UserQualification (many-to-many User <-> Qualification)
            modelBuilder.Entity<UserQualification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserQualifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Qualification)
                    .WithMany(q => q.UserQualifications)
                    .HasForeignKey(e => e.QualificationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaskTemplateQualification (many-to-many TaskTemplate <-> Qualification)
            modelBuilder.Entity<TaskTemplateQualification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.TaskTemplate)
                    .WithMany(t => t.TaskTemplateQualifications)
                    .HasForeignKey(e => e.TaskTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Qualification)
                    .WithMany(q => q.TaskTemplateQualifications)
                    .HasForeignKey(e => e.QualificationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // MaintenanceScheduleQualification (many-to-many MaintenanceSchedule <-> Qualification)
            modelBuilder.Entity<MaintenanceScheduleQualification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.MaintenanceSchedule)
                    .WithMany(ms => ms.MaintenanceScheduleQualifications)
                    .HasForeignKey(e => e.MaintenanceScheduleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Qualification)
                    .WithMany(q => q.MaintenanceScheduleQualifications)
                    .HasForeignKey(e => e.QualificationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Sector (Secteurs d'activité)
            modelBuilder.Entity<Sector>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(10);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Industry (Métiers)
            modelBuilder.Entity<Industry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(10);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Sector)
                    .WithMany(s => s.Industries)
                    .HasForeignKey(e => e.SectorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PredefinedSector (Table de référence maître des secteurs prédéfinis)
            modelBuilder.Entity<PredefinedSector>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // PredefinedIndustry (Table de référence maître des métiers prédéfinis)
            modelBuilder.Entity<PredefinedIndustry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasOne(e => e.PredefinedSector)
                    .WithMany()
                    .HasForeignKey(e => e.PredefinedSectorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PredefinedQualification (Qualifications prédéfinies avec RNCP/RS)
            modelBuilder.Entity<PredefinedQualification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.RncpCode).HasMaxLength(20);
                entity.Property(e => e.RsCode).HasMaxLength(20);
                entity.Property(e => e.FranceCompetencesUrl).HasMaxLength(500);
                entity.Property(e => e.Certificateur).HasMaxLength(500);
                entity.Property(e => e.Color).HasMaxLength(50);
                entity.Property(e => e.Icon).HasMaxLength(10);
            });

            // PredefinedQualificationSector (many-to-many: Qualification <-> Secteur)
            modelBuilder.Entity<PredefinedQualificationSector>(entity =>
            {
                // Clé composite sur les deux FK
                entity.HasKey(e => new { e.PredefinedQualificationId, e.PredefinedSectorId });

                entity.HasOne(e => e.PredefinedQualification)
                    .WithMany(q => q.QualificationSectors)
                    .HasForeignKey(e => e.PredefinedQualificationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PredefinedSector)
                    .WithMany()
                    .HasForeignKey(e => e.PredefinedSectorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MaintenanceSchedule (Gammes de maintenance)
            modelBuilder.Entity<MaintenanceSchedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Frequency).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RequiredQualification).HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.DefaultTeam)
                    .WithMany()
                    .HasForeignKey(e => e.DefaultTeamId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.DefaultUser)
                    .WithMany()
                    .HasForeignKey(e => e.DefaultUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // MaintenanceTask (Tâches dans une gamme)
            modelBuilder.Entity<MaintenanceTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TaskType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.MeasurementUnit).HasMaxLength(20);
                entity.HasOne(e => e.MaintenanceSchedule)
                    .WithMany(ms => ms.Tasks)
                    .HasForeignKey(e => e.MaintenanceScheduleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MaintenanceExecution (Historique des maintenances)
            modelBuilder.Entity<MaintenanceExecution>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MaintenanceType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.EquipmentCondition).HasMaxLength(20);
                entity.Property(e => e.NextMaintenancePriority).HasMaxLength(20);
                entity.HasOne(e => e.MaintenanceSchedule)
                    .WithMany(ms => ms.Executions)
                    .HasForeignKey(e => e.MaintenanceScheduleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ScheduledTask)
                    .WithOne()
                    .HasForeignKey<MaintenanceExecution>(e => e.ScheduledTaskId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Team)
                    .WithMany()
                    .HasForeignKey(e => e.TeamId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // RfidChipStatusHistory (Historique des changements de statut RFID)
            modelBuilder.Entity<RfidChipStatusHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FromStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ToStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.HasOne(e => e.RfidChip)
                    .WithMany(r => r.StatusHistory)
                    .HasForeignKey(e => e.RfidChipId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Supplier (Fournisseur)
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContactName).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.Country).HasMaxLength(2).IsRequired();
                entity.Property(e => e.Siret).HasMaxLength(14); // Nullable - France uniquement
                entity.Property(e => e.VatNumber).HasMaxLength(20); // Nullable - UE
                entity.Property(e => e.TaxId).HasMaxLength(50); // Nullable - Hors UE
                entity.Property(e => e.Website).HasMaxLength(500);
                entity.Property(e => e.PaymentTerms).HasMaxLength(50);
                entity.HasIndex(e => e.Siret).IsUnique().HasFilter("[Siret] IS NOT NULL");
                entity.HasIndex(e => e.VatNumber).IsUnique().HasFilter("[VatNumber] IS NOT NULL");
                entity.HasMany(e => e.Orders)
                    .WithOne(o => o.Supplier)
                    .HasForeignKey(o => o.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SupplierOrder (Commande fournisseur)
            modelBuilder.Entity<SupplierOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Supplier)
                    .WithMany(s => s.Orders)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(e => e.Lines)
                    .WithOne(l => l.SupplierOrder)
                    .HasForeignKey(l => l.SupplierOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(e => e.RfidChips)
                    .WithOne()
                    .HasForeignKey(r => r.SupplierOrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // SupplierOrderLine (Ligne de commande fournisseur)
            modelBuilder.Entity<SupplierOrderLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.SupplierOrder)
                    .WithMany(o => o.Lines)
                    .HasForeignKey(e => e.SupplierOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // EquipmentCategory (Catégories d'équipements personnalisables)
            modelBuilder.Entity<EquipmentCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(10);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // EquipmentType (Types d'équipements dans les catégories)
            modelBuilder.Entity<EquipmentType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Icon).HasMaxLength(10);
                entity.HasOne(e => e.EquipmentCategory)
                    .WithMany(c => c.EquipmentTypes)
                    .HasForeignKey(e => e.EquipmentCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // EquipmentStatus (Statuts d'équipements personnalisables)
            modelBuilder.Entity<EquipmentStatus>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(10);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // FavoriteManufacturer (Fabricants favoris)
            modelBuilder.Entity<FavoriteManufacturer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(200);
                entity.Property(e => e.ContactEmail).HasMaxLength(100);
                entity.Property(e => e.ContactPhone).HasMaxLength(50);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PredefinedEquipmentCategory (Catégories d'équipements prédéfinies)
            modelBuilder.Entity<PredefinedEquipmentCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.Icon).HasMaxLength(10);
            });

            // PredefinedEquipmentType (Types d'équipements prédéfinis)
            modelBuilder.Entity<PredefinedEquipmentType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Icon).HasMaxLength(10);
                entity.HasOne(e => e.PredefinedEquipmentCategory)
                    .WithMany()
                    .HasForeignKey(e => e.PredefinedEquipmentCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // HomeContent (Contenu éditable de la page d'accueil)
            modelBuilder.Entity<HomeContent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasColumnType("jsonb");
                entity.Property(e => e.IsPublished).HasDefaultValue(false);
                entity.Property(e => e.Version).HasDefaultValue(1);
                entity.Property(e => e.CreatedAt).HasDefaultValue(DateTime.UtcNow);
                entity.Property(e => e.UpdatedAt).HasDefaultValue(DateTime.UtcNow);
            });

            // ========== CONFIGURATION ENTITÉS NUCLÉAIRES DMTT ==========

            // Weld (Soudures)
            modelBuilder.Entity<Weld>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => new { e.CustomerId, e.Reference }).IsUnique();
                entity.Property(e => e.WeldingParameters).HasColumnType("jsonb");
                entity.Property(e => e.Photos).HasColumnType("jsonb");
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.DMOS)
                    .WithMany(d => d.Welds)
                    .HasForeignKey(e => e.DMOSId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Welder)
                    .WithMany()
                    .HasForeignKey(e => e.WelderId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CCPUValidator)
                    .WithMany()
                    .HasForeignKey(e => e.CCPUValidatorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Material (Matériaux)
            modelBuilder.Entity<Material>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.CustomerId, e.Reference }).IsUnique();
                entity.Property(e => e.Dimensions).HasColumnType("jsonb");
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CCPUValidator)
                    .WithMany()
                    .HasForeignKey(e => e.CCPUValidatorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Subcontractor)
                    .WithMany()
                    .HasForeignKey(e => e.SubcontractorId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // DMOS
            modelBuilder.Entity<DMOS>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.CustomerId, e.Reference }).IsUnique();
                entity.Property(e => e.WeldingParameters).HasColumnType("jsonb");
                entity.Property(e => e.ApplicableStandards).HasColumnType("jsonb");
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // NDTProgram (Programmes CND)
            modelBuilder.Entity<NDTProgram>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.CustomerId, e.Reference }).IsUnique();
                entity.Property(e => e.RequiredControls).HasColumnType("jsonb");
                entity.Property(e => e.AcceptanceCriteria).HasColumnType("jsonb");
                entity.Property(e => e.ApplicableStandards).HasColumnType("jsonb");
                entity.Property(e => e.ControlSequence).HasColumnType("jsonb");
                entity.Property(e => e.AIInputData).HasColumnType("jsonb");
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // NDTControl (Contrôles CND)
            modelBuilder.Entity<NDTControl>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ControlType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DefectsFound).HasColumnType("jsonb");
                entity.Property(e => e.ControlParameters).HasColumnType("jsonb");
                entity.Property(e => e.Photos).HasColumnType("jsonb");
                entity.Property(e => e.EnvironmentalConditions).HasColumnType("jsonb");
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Weld)
                    .WithMany(w => w.NDTControls)
                    .HasForeignKey(e => e.WeldId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.NDTProgram)
                    .WithMany(p => p.NDTControls)
                    .HasForeignKey(e => e.NDTProgramId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Controller)
                    .WithMany()
                    .HasForeignKey(e => e.ControllerId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                // Relation one-to-one avec NonConformity (NDTControl est le principal)
                entity.HasOne(e => e.NonConformity)
                    .WithOne(nc => nc.NDTControl)
                    .HasForeignKey<NDTControl>(e => e.NonConformityId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // NonConformity (FNC)
            modelBuilder.Entity<NonConformity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => new { e.CustomerId, e.Reference }).IsUnique();
                entity.Property(e => e.Attachments).HasColumnType("jsonb");
                entity.Property(e => e.ActionHistory).HasColumnType("jsonb");
                // Ignorer NDTControlId car relation gérée depuis NDTControl avec NonConformityId
                entity.Ignore(e => e.NDTControlId);
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Weld)
                    .WithMany(w => w.NonConformities)
                    .HasForeignKey(e => e.WeldId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Material)
                    .WithMany(m => m.NonConformities)
                    .HasForeignKey(e => e.MaterialId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ActionResponsible)
                    .WithMany()
                    .HasForeignKey(e => e.ActionResponsibleId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ClosedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ClosedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WelderQualification
            modelBuilder.Entity<WelderQualification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QualificationNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.QualificationType).IsRequired().HasMaxLength(30);
                entity.Property(e => e.AIExtractedData).HasColumnType("jsonb");
                entity.Property(e => e.AIWarnings).HasColumnType("jsonb");
                entity.HasIndex(e => new { e.UserId, e.QualificationType, e.QualificationNumber }).IsUnique();
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.WelderQualifications)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ValidatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ValidatedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TechnicalDocument
            modelBuilder.Entity<TechnicalDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(30);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Tags).HasColumnType("jsonb");
                entity.Property(e => e.AIExtractedMetadata).HasColumnType("jsonb");
                entity.Property(e => e.AccessRoles).HasColumnType("jsonb");
                entity.HasIndex(e => new { e.CustomerId, e.Reference, e.Version }).IsUnique();
                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Asset)
                    .WithMany()
                    .HasForeignKey(e => e.AssetId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.UploadedBy)
                    .WithMany()
                    .HasForeignKey(e => e.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
