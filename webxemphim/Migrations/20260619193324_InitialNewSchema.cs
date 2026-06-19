using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace webxemphim.Migrations
{
    /// <inheritdoc />
    public partial class InitialNewSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    acc_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acc_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    acc_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    acc_email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    acc_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    wal_balance = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    prof_vip_exp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    prof_avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    prof_phone = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    prof_address = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    acc_stamp = table.Column<int>(type: "integer", nullable: false),
                    acc_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acc_locked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.acc_id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    log_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    log_cat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    log_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    log_msg = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    log_acc = table.Column<int>(type: "integer", nullable: true),
                    log_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    log_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    log_detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.log_id);
                });

            migrationBuilder.CreateTable(
                name: "bill_header",
                columns: table => new
                {
                    bill_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_code = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bill_acc = table.Column<int>(type: "integer", nullable: false),
                    bill_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bill_email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    bill_tx = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bill_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bill_service = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bild_amount = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bild_currency = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bild_vnd = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bild_before = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bild_after = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bill_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    bill_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bill_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bill_header", x => x.bill_id);
                });

            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    cat_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cat_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cat_desc = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cat_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cat_active = table.Column<bool>(type: "boolean", nullable: false),
                    cat_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.cat_id);
                });

            migrationBuilder.CreateTable(
                name: "currency",
                columns: table => new
                {
                    cur_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cur_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cur_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cur_symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cur_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    cur_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cur_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency", x => x.cur_id);
                });

            migrationBuilder.CreateTable(
                name: "favorite",
                columns: table => new
                {
                    fav_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fav_acc = table.Column<int>(type: "integer", nullable: false),
                    fav_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fav_mov = table.Column<int>(type: "integer", nullable: false),
                    fav_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fav_image = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    fav_added = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite", x => x.fav_id);
                });

            migrationBuilder.CreateTable(
                name: "login_attempt",
                columns: table => new
                {
                    la_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    la_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    la_fail = table.Column<int>(type: "integer", nullable: false),
                    la_last = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    la_locked = table.Column<bool>(type: "boolean", nullable: false),
                    la_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_attempt", x => x.la_id);
                });

            migrationBuilder.CreateTable(
                name: "movie_info",
                columns: table => new
                {
                    mov_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mov_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mov_desc = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    mov_genre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mov_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mov_year = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    media_image = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    media_video = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    mov_vip = table.Column<bool>(type: "boolean", nullable: false),
                    mov_active = table.Column<bool>(type: "boolean", nullable: false),
                    mov_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mov_director = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mov_actors = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mov_duration = table.Column<int>(type: "integer", nullable: true),
                    mov_views = table.Column<int>(type: "integer", nullable: false),
                    mov_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movie_info", x => x.mov_id);
                });

            migrationBuilder.CreateTable(
                name: "tx_header",
                columns: table => new
                {
                    tx_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tx_acc = table.Column<int>(type: "integer", nullable: false),
                    tx_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tx_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    txd_amount = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    txd_currency = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    txd_vnd = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    tx_desc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tx_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tx_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tx_header", x => x.tx_id);
                });

            migrationBuilder.CreateTable(
                name: "watch_history",
                columns: table => new
                {
                    wh_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wh_acc = table.Column<int>(type: "integer", nullable: false),
                    wh_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    wh_mov = table.Column<int>(type: "integer", nullable: false),
                    wh_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    wh_image = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    wh_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    wh_duration = table.Column<int>(type: "integer", nullable: false),
                    wh_done = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watch_history", x => x.wh_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_log_at",
                table: "audit_log",
                column: "log_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_log_cat",
                table: "audit_log",
                column: "log_cat");

            migrationBuilder.CreateIndex(
                name: "IX_login_attempt_la_key",
                table: "login_attempt",
                column: "la_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "bill_header");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "currency");

            migrationBuilder.DropTable(
                name: "favorite");

            migrationBuilder.DropTable(
                name: "login_attempt");

            migrationBuilder.DropTable(
                name: "movie_info");

            migrationBuilder.DropTable(
                name: "tx_header");

            migrationBuilder.DropTable(
                name: "watch_history");
        }
    }
}
