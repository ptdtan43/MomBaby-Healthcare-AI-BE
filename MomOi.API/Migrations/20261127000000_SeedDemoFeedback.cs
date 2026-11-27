using System.IO;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MomOi.API.Data;

namespace MomOi.API.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261127000000_SeedDemoFeedback")]
public class SeedDemoFeedback : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(SeedDemoFeedback).Assembly
            .GetManifestResourceStream("MomOi.API.SeedDemoFeedback.sql")
            ?? throw new InvalidOperationException("Feedback seed SQL resource is missing.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM notification_alerts
            WHERE user_id IN (
                SELECT 'demo-feedback-' || lpad(n::text, 2, '0')
                FROM generate_series(1, 30) AS n
            );
            DELETE FROM "AspNetUsers"
            WHERE id IN (
                SELECT 'demo-feedback-' || lpad(n::text, 2, '0')
                FROM generate_series(1, 30) AS n
            );
            """);
    }
}
