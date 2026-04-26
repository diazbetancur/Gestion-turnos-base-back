using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CC.Infrastructure.Configurations
{
    public class DBContext : IdentityDbContext<User, Role, Guid>, IQueryableUnitOfWork
    {
        public DBContext(DbContextOptions<DBContext> options)
        : base(options)
        {
        }

        /// <summary>
        /// Permission
        /// </summary>
        public DbSet<Permission> Permissions { get; set; }

        /// <summary>
        /// PermissionRole
        /// </summary>
        public DbSet<RolePermission> RolePermissions { get; set; }

        /// <summary>
        /// WorkArea
        /// </summary>
        public DbSet<WorkArea> WorkAreas { get; set; }

        /// <summary>
        /// Workstation
        /// </summary>
        public DbSet<Workstation> Workstations { get; set; }

        /// <summary>
        /// UserWorkstations
        /// </summary>
        public DbSet<UserWorkstation> UserWorkstations { get; set; }

        /// <summary>
        /// HireTypes
        /// </summary>
        public DbSet<HireType> HireTypes { get; set; }

        /// <summary>
        /// UserActivityLog
        /// </summary>
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        /// <summary>
        /// ShiftType
        /// </summary>
        public DbSet<ShiftType> ShiftTypes { get; set; }

        /// <summary>
        /// HybridWorkstation
        /// </summary>
        public DbSet<HybridWorkstation> HybridWorkstations { get; set; }

        /// <summary>
        /// EmployeeScheduleRestriction
        /// </summary>
        public DbSet<EmployeeScheduleRestriction> EmployeeScheduleRestrictions { get; set; }

        /// <summary>
        /// AbsenteeismType
        /// </summary>
        public DbSet<AbsenteeismType> AbsenteeismTypes { get; set; }

        /// <summary>
        /// UserAbsenteeism
        /// </summary>
        public DbSet<UserAbsenteeism> UserAbsenteeisms { get; set; }

        /// <summary>
        /// EmployeeScheduleException
        /// </summary>
        public DbSet<EmployeeScheduleException> EmployeeScheduleExceptions { get; set; }

        /// <summary>
        /// LawRestriction
        /// </summary>
        public DbSet<LawRestriction> LawRestrictions { get; set; }

        /// <summary>
        /// WorkstationDemandTemplate
        /// </summary>
        public DbSet<WorkstationDemandTemplate> WorkstationDemandTemplates { get; set; }

        /// <summary>
        /// WorkstationDemand
        /// </summary>
        public DbSet<WorkstationDemand> WorkstationDemands { get; set; }

        /// <summary>
        /// EmployeeShiftTypeRestriction
        /// </summary>
        public DbSet<EmployeeShiftTypeRestriction> EmployeeShiftTypeRestrictions { get; set; }

        /// <summary>
        /// Schedule
        /// </summary>
        public DbSet<Schedule> Schedules { get; set; }

        /// <summary>
        /// UserShift
        /// </summary>
        public DbSet<UserShift> UserShifts { get; set; }

        /// <summary>
        /// ScheduleGap
        /// </summary>
        public DbSet<ScheduleGap> ScheduleGaps { get; set; }

        /// <summary>
        /// ScheduleSuggestion
        /// </summary>
        public DbSet<ScheduleSuggestion> ScheduleSuggestions { get; set; }

        /// <summary>
        /// ScheduleQualityShiftScore
        /// </summary>
        public DbSet<ScheduleQualityShiftScore> ScheduleQualityShiftScores { get; set; }

        /// <summary>
        /// IgnoredRestriction
        /// </summary>
        public DbSet<IgnoredRestriction> IgnoredRestrictions { get; set; }

        /// <summary>
        /// SigningCofiguration
        /// </summary>
        public DbSet<SigningCofiguration> SigningCofigurations { get; set; }

        /// <summary>
        /// SigningCofiguration
        /// </summary>
        public DbSet<Signing> Signings { get; set; }

        /// <summary>
        /// AuditLog - Registro de auditoría de cambios
        /// </summary>
        public DbSet<AuditLog> AuditLogs { get; set; }

        // TODO: Revisar si es necesario esto

        /// <summary>
        /// License
        /// </summary>
        public DbSet<License> Licenses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<User>().Property(p => p.FirstName).HasMaxLength(20);
            modelBuilder.Entity<User>().Property(p => p.LastName).HasMaxLength(20);

            modelBuilder.Entity<Role>().HasKey(c => c.Id);
            modelBuilder.Entity<Role>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            modelBuilder.Entity<Permission>().HasKey(c => c.Id);
            modelBuilder.Entity<Permission>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<Permission>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<RolePermission>().HasKey(c => c.Id);
            modelBuilder.Entity<RolePermission>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<RolePermission>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<WorkArea>().HasKey(c => c.Id);
            modelBuilder.Entity<WorkArea>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<WorkArea>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Workstation>().HasKey(c => c.Id);
            modelBuilder.Entity<Workstation>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<Workstation>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<UserWorkstation>().HasKey(c => c.Id);
            modelBuilder.Entity<UserWorkstation>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<UserWorkstation>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<HireType>().HasKey(c => c.Id);
            modelBuilder.Entity<HireType>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<HireType>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<UserActivityLog>().HasKey(c => c.Id);
            modelBuilder.Entity<UserActivityLog>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<UserActivityLog>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<ShiftType>().HasKey(c => c.Id);
            modelBuilder.Entity<ShiftType>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<ShiftType>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<HybridWorkstation>().HasKey(c => c.Id);
            modelBuilder.Entity<HybridWorkstation>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<HybridWorkstation>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<HybridWorkstation>().HasKey(c => c.Id);
            modelBuilder.Entity<HybridWorkstation>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<HybridWorkstation>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<EmployeeScheduleRestriction>().HasKey(c => c.Id);
            modelBuilder.Entity<EmployeeScheduleRestriction>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<EmployeeScheduleRestriction>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<AbsenteeismType>().HasKey(c => c.Id);
            modelBuilder.Entity<AbsenteeismType>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<AbsenteeismType>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<UserAbsenteeism>().HasKey(c => c.Id);
            modelBuilder.Entity<UserAbsenteeism>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<UserAbsenteeism>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<EmployeeScheduleException>().HasKey(c => c.Id);
            modelBuilder.Entity<EmployeeScheduleException>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<EmployeeScheduleException>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<LawRestriction>().HasKey(c => c.Id);
            modelBuilder.Entity<LawRestriction>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<LawRestriction>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<WorkstationDemandTemplate>().HasKey(c => c.Id);
            modelBuilder.Entity<WorkstationDemandTemplate>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<WorkstationDemandTemplate>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<WorkstationDemand>().HasKey(c => c.Id);
            modelBuilder.Entity<WorkstationDemand>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<WorkstationDemand>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<EmployeeShiftTypeRestriction>().HasKey(c => c.Id);
            modelBuilder.Entity<EmployeeShiftTypeRestriction>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<EmployeeShiftTypeRestriction>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Schedule>().HasKey(c => c.Id);
            modelBuilder.Entity<Schedule>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<Schedule>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Schedule>().HasKey(c => c.Id);
            modelBuilder.Entity<Schedule>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<Schedule>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Signing>().HasKey(c => c.Id);
            modelBuilder.Entity<Signing>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<Signing>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<SigningCofiguration>().HasKey(c => c.Id);
            modelBuilder.Entity<SigningCofiguration>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<SigningCofiguration>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            // TODO: Revisar si es necesario esto

            modelBuilder.Entity<UserShift>().HasKey(c => c.Id);
            modelBuilder.Entity<UserShift>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<UserShift>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<ScheduleGap>().HasKey(c => c.Id);
            modelBuilder.Entity<ScheduleGap>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.DateCreated).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.Date).HasColumnType("date");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.StartTime).HasColumnType("time without time zone");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.EndTime).HasColumnType("time without time zone");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.GapExplanation).HasColumnType("text");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.GapCategory).HasColumnType("text");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.Token).HasColumnType("text");
            modelBuilder.Entity<ScheduleGap>().Property(e => e.IsPostAi).HasDefaultValue(false);
            modelBuilder.Entity<ScheduleGap>()
                .HasOne(e => e.Workstation)
                .WithMany()
                .HasForeignKey(e => e.WorkstationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScheduleSuggestion>().HasKey(c => c.Id);
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.DateCreated).HasColumnName("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.Token).HasMaxLength(100);
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.WeekStart).HasColumnType("date");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.WeekEnd).HasColumnType("date");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.Tipo).HasMaxLength(50);
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.Prioridad).HasMaxLength(20);
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.Titulo).HasMaxLength(200);
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.Descripcion).HasColumnType("text");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.ImpactoEsperado).HasColumnType("jsonb");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.MejoraEstimada).HasColumnType("numeric(5,2)");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.Detalles).HasColumnType("jsonb");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.EmpleadosInvolucrados).HasColumnType("jsonb");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.WorkstationsAfectadas).HasColumnType("jsonb");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.DiasAfectados).HasColumnType("jsonb");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.AccionesConcretas).HasColumnType("jsonb");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.Implementada).HasDefaultValue(false);
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.FechaImplementacion).HasColumnType("timestamp without time zone");
            modelBuilder.Entity<ScheduleSuggestion>().Property(e => e.IsPostAi).HasDefaultValue(false);

            modelBuilder.Entity<ScheduleQualityShiftScore>().HasKey(c => c.Id);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.Token).HasColumnType("text");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.DemandId).HasColumnType("uuid");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.Date).HasColumnType("date");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.WorkstationName).HasColumnType("text");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.StartTime).HasColumnType("interval");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.EndTime).HasColumnType("interval");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.Demanded).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.Covered).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.Unmet).HasColumnType("numeric(10,2)").HasDefaultValue(0m);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.CoverageScore).HasColumnType("numeric(5,2)").HasDefaultValue(100m);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.FairnessScore).HasColumnType("numeric(5,2)").HasDefaultValue(100m);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.RulesScore).HasColumnType("numeric(5,2)").HasDefaultValue(100m);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.Score).HasColumnType("numeric(5,2)").HasDefaultValue(100m);
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.Metrics).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.ConfigSnapshot).HasColumnType("jsonb");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.DateCreated).HasColumnType("timestamp without time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<ScheduleQualityShiftScore>().Property(e => e.IsPostAi).HasDefaultValue(false);

            modelBuilder.Entity<IgnoredRestriction>().HasKey(c => c.Id);
            modelBuilder.Entity<IgnoredRestriction>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<IgnoredRestriction>().Property(e => e.Date).HasColumnType("date");
            modelBuilder.Entity<IgnoredRestriction>().Property(e => e.Observation).HasColumnType("text");
            modelBuilder.Entity<IgnoredRestriction>().Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<IgnoredRestriction>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración de AuditLog
            modelBuilder.Entity<AuditLog>().HasKey(c => c.Id);
            modelBuilder.Entity<AuditLog>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<AuditLog>().Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.EntityName);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.EntityId);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.UserId);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
            modelBuilder.Entity<AuditLog>().HasIndex(a => new { a.EntityName, a.EntityId });

            modelBuilder.HasDefaultSchema("Management");
            DisableCascadingDelete(modelBuilder);
        }

        private void DisableCascadingDelete(ModelBuilder modelBuilder)
        {
            var relationship = modelBuilder.Model.GetEntityTypes()
                .Where(e => !e.ClrType.Namespace.StartsWith("Microsoft.AspNetCore.Identity"))
                .SelectMany(e => e.GetForeignKeys());

            foreach (var r in relationship)
            {
                r.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        public void Commit()
        {
            try
            {
                SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ex.Entries.Single().Reload();
            }
        }

        public async Task CommitAsync()
        {
            try
            {
                await SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await ex.Entries.Single().ReloadAsync().ConfigureAwait(false);
            }
        }

        public void DetachLocal<TEntity>(TEntity entity, EntityState state) where TEntity : class
        {
            if (entity is null)
            {
                return;
            }

            var local = Set<TEntity>().Local.ToList();

            if (local?.Any() ?? false)
            {
                local.ForEach(item =>
                {
                    Entry(item).State = EntityState.Detached;
                });
            }

            Entry(entity).State = state;
        }

        public DbContext GetContext()
        {
            return this;
        }

        public DbSet<TEntity> GetSet<TEntity>() where TEntity : class
        {
            return Set<TEntity>();
        }
    }
}