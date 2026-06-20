-- ============================================================
-- DATABASE REDESIGN v2 - webxemphim
-- Nguyen tac:
--   1. Moi bang doc lap - khong co FOREIGN KEY constraint
--   2. Moi bang co prefix rieng cho cot
--   3. Cot ngay/thoi gian tach sang bang *_date rieng
--   4. Tach bang chi tiet (profile, wallet, media, tx_detail, bill_detail)
-- ============================================================

SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

-- ============================================================
-- XOA TOAN BO TABLE CU (Railway + local + EF migrations)
-- ============================================================
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

-- ============================================================
-- NHOM 1: USER (account + profile + wallet + date tables)
-- ============================================================

DROP TABLE IF EXISTS "account" CASCADE;
CREATE TABLE "account" (
    "acc_id"        SERIAL        PRIMARY KEY,
    "acc_name"      VARCHAR(100)  NOT NULL,
    "acc_hash"      VARCHAR(256)  NOT NULL,
    "acc_email"     VARCHAR(512)  NOT NULL,
    "acc_role"      VARCHAR(50)   NOT NULL DEFAULT 'User',
    "acc_locked"    BOOLEAN       NOT NULL DEFAULT FALSE,
    "acc_stamp"     INTEGER       NOT NULL DEFAULT 0
);

DROP TABLE IF EXISTS "account_date" CASCADE;
CREATE TABLE "account_date" (
    "accd_id"       SERIAL        PRIMARY KEY,
    "accd_acc"      INTEGER       NOT NULL,
    "accd_created"  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

DROP TABLE IF EXISTS "profile" CASCADE;
CREATE TABLE "profile" (
    "prof_id"       SERIAL        PRIMARY KEY,
    "prof_acc"      INTEGER       NOT NULL,
    "prof_name"     VARCHAR(100)  NOT NULL,
    "prof_phone"    VARCHAR(512),
    "prof_address"  VARCHAR(1024),
    "prof_avatar"   VARCHAR(500)
);

DROP TABLE IF EXISTS "profile_date" CASCADE;
CREATE TABLE "profile_date" (
    "profd_id"      SERIAL        PRIMARY KEY,
    "profd_prof"    INTEGER       NOT NULL,
    "profd_vip_exp" TIMESTAMPTZ,
    "profd_updated" TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

DROP TABLE IF EXISTS "wallet" CASCADE;
CREATE TABLE "wallet" (
    "wal_id"        SERIAL        PRIMARY KEY,
    "wal_acc"       INTEGER       NOT NULL,
    "wal_name"      VARCHAR(100)  NOT NULL,
    "wal_balance"   VARCHAR(512)  NOT NULL DEFAULT ''
);

DROP TABLE IF EXISTS "wallet_date" CASCADE;
CREATE TABLE "wallet_date" (
    "wald_id"       SERIAL        PRIMARY KEY,
    "wald_wal"      INTEGER       NOT NULL,
    "wald_updated"  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ============================================================
-- NHOM 2: MOVIE (movie_info + movie_media + date tables)
-- ============================================================

DROP TABLE IF EXISTS "movie_info" CASCADE;
CREATE TABLE "movie_info" (
    "mov_id"        SERIAL        PRIMARY KEY,
    "mov_title"     VARCHAR(200)  NOT NULL,
    "mov_desc"      VARCHAR(1000) NOT NULL DEFAULT '',
    "mov_genre"     VARCHAR(100)  NOT NULL DEFAULT '',
    "mov_country"   VARCHAR(100)  NOT NULL DEFAULT '',
    "mov_year"      VARCHAR(50)   NOT NULL DEFAULT '',
    "mov_duration"  INTEGER,
    "mov_actors"    VARCHAR(500),
    "mov_director"  VARCHAR(100),
    "mov_category"  VARCHAR(100)  NOT NULL DEFAULT '',
    "mov_vip"       BOOLEAN       NOT NULL DEFAULT FALSE,
    "mov_active"    BOOLEAN       NOT NULL DEFAULT TRUE,
    "mov_views"     INTEGER       NOT NULL DEFAULT 0
);

DROP TABLE IF EXISTS "movie_date" CASCADE;
CREATE TABLE "movie_date" (
    "movd_id"       SERIAL        PRIMARY KEY,
    "movd_mov"      INTEGER       NOT NULL,
    "movd_created"  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

DROP TABLE IF EXISTS "movie_media" CASCADE;
CREATE TABLE "movie_media" (
    "media_id"      SERIAL        PRIMARY KEY,
    "media_mov"     INTEGER       NOT NULL,
    "media_title"   VARCHAR(200)  NOT NULL,
    "media_image"   VARCHAR(1024) NOT NULL DEFAULT '',
    "media_video"   VARCHAR(1024) NOT NULL DEFAULT ''
);

DROP TABLE IF EXISTS "media_date" CASCADE;
CREATE TABLE "media_date" (
    "mediad_id"     SERIAL        PRIMARY KEY,
    "mediad_media"  INTEGER       NOT NULL,
    "mediad_updated" TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ============================================================
-- NHOM 3: TRANSACTION (tx_header + tx_detail + tx_date)
-- ============================================================

DROP TABLE IF EXISTS "tx_header" CASCADE;
CREATE TABLE "tx_header" (
    "tx_id"         SERIAL        PRIMARY KEY,
    "tx_acc"        INTEGER       NOT NULL,
    "tx_name"       VARCHAR(100)  NOT NULL DEFAULT '',
    "tx_type"       VARCHAR(50)   NOT NULL,
    "tx_desc"       VARCHAR(200)  NOT NULL DEFAULT '',
    "tx_status"     VARCHAR(50)   NOT NULL DEFAULT 'Pending'
);

DROP TABLE IF EXISTS "tx_date" CASCADE;
CREATE TABLE "tx_date" (
    "txdate_id"     SERIAL        PRIMARY KEY,
    "txdate_tx"     INTEGER       NOT NULL,
    "txdate_created" TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

DROP TABLE IF EXISTS "tx_detail" CASCADE;
CREATE TABLE "tx_detail" (
    "txd_id"        SERIAL        PRIMARY KEY,
    "txd_tx"        INTEGER       NOT NULL,
    "txd_amount"    VARCHAR(256)  NOT NULL,
    "txd_currency"  VARCHAR(256)  NOT NULL,
    "txd_vnd"       VARCHAR(256)  NOT NULL
);

-- ============================================================
-- NHOM 4: BILL (bill_header + bill_detail + bill_date)
-- ============================================================

DROP TABLE IF EXISTS "bill_header" CASCADE;
CREATE TABLE "bill_header" (
    "bill_id"       SERIAL        PRIMARY KEY,
    "bill_code"     VARCHAR(256)  NOT NULL,
    "bill_acc"      INTEGER       NOT NULL,
    "bill_name"     VARCHAR(100)  NOT NULL,
    "bill_email"    VARCHAR(512)  NOT NULL,
    "bill_tx"       VARCHAR(256)  NOT NULL,
    "bill_type"     VARCHAR(50)   NOT NULL,
    "bill_service"  VARCHAR(200)  NOT NULL,
    "bill_status"   VARCHAR(50)   NOT NULL DEFAULT 'Completed',
    "bill_note"     VARCHAR(500)  NOT NULL DEFAULT ''
);

DROP TABLE IF EXISTS "bill_date" CASCADE;
CREATE TABLE "bill_date" (
    "billd_id"      SERIAL        PRIMARY KEY,
    "billd_bill"    INTEGER       NOT NULL,
    "billd_created" TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

DROP TABLE IF EXISTS "bill_detail" CASCADE;
CREATE TABLE "bill_detail" (
    "bild_id"       SERIAL        PRIMARY KEY,
    "bild_bill"     INTEGER       NOT NULL,
    "bild_amount"   VARCHAR(256)  NOT NULL,
    "bild_currency" VARCHAR(256)  NOT NULL,
    "bild_vnd"      VARCHAR(256)  NOT NULL,
    "bild_before"   VARCHAR(256)  NOT NULL,
    "bild_after"    VARCHAR(256)  NOT NULL
);

-- ============================================================
-- NHOM 5: CATEGORY
-- ============================================================

DROP TABLE IF EXISTS "category" CASCADE;
CREATE TABLE "category" (
    "cat_id"        SERIAL        PRIMARY KEY,
    "cat_name"      VARCHAR(100)  NOT NULL,
    "cat_desc"      VARCHAR(500),
    "cat_type"      VARCHAR(50),
    "cat_active"    BOOLEAN       NOT NULL DEFAULT TRUE,
    "cat_order"     INTEGER       NOT NULL DEFAULT 0
);

-- ============================================================
-- NHOM 6: CURRENCY + currency_date
-- ============================================================

DROP TABLE IF EXISTS "currency" CASCADE;
CREATE TABLE "currency" (
    "cur_id"        SERIAL        PRIMARY KEY,
    "cur_code"      VARCHAR(10)   NOT NULL,
    "cur_name"      VARCHAR(50)   NOT NULL,
    "cur_symbol"    VARCHAR(10)   NOT NULL,
    "cur_rate"      NUMERIC       NOT NULL DEFAULT 1,
    "cur_active"    BOOLEAN       NOT NULL DEFAULT TRUE
);

DROP TABLE IF EXISTS "currency_date" CASCADE;
CREATE TABLE "currency_date" (
    "curd_id"       SERIAL        PRIMARY KEY,
    "curd_cur"      INTEGER       NOT NULL,
    "curd_updated"  TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ============================================================
-- NHOM 7: FAVORITE + favorite_date
-- ============================================================

DROP TABLE IF EXISTS "favorite" CASCADE;
CREATE TABLE "favorite" (
    "fav_id"        SERIAL        PRIMARY KEY,
    "fav_acc"       INTEGER       NOT NULL,
    "fav_name"      VARCHAR(100)  NOT NULL,
    "fav_mov"       INTEGER       NOT NULL,
    "fav_title"     VARCHAR(200)  NOT NULL,
    "fav_image"     VARCHAR(1024) NOT NULL DEFAULT ''
);

DROP TABLE IF EXISTS "favorite_date" CASCADE;
CREATE TABLE "favorite_date" (
    "favd_id"       SERIAL        PRIMARY KEY,
    "favd_fav"      INTEGER       NOT NULL,
    "favd_added"    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ============================================================
-- NHOM 8: WATCH HISTORY + watch_date
-- ============================================================

DROP TABLE IF EXISTS "watch_history" CASCADE;
CREATE TABLE "watch_history" (
    "wh_id"         SERIAL        PRIMARY KEY,
    "wh_acc"        INTEGER       NOT NULL,
    "wh_name"       VARCHAR(100)  NOT NULL,
    "wh_mov"        INTEGER       NOT NULL,
    "wh_title"      VARCHAR(200)  NOT NULL,
    "wh_image"      VARCHAR(1024) NOT NULL DEFAULT '',
    "wh_duration"   INTEGER       NOT NULL DEFAULT 0,
    "wh_done"       BOOLEAN       NOT NULL DEFAULT FALSE
);

DROP TABLE IF EXISTS "watch_date" CASCADE;
CREATE TABLE "watch_date" (
    "whd_id"        SERIAL        PRIMARY KEY,
    "whd_wh"        INTEGER       NOT NULL,
    "whd_at"        TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ============================================================
-- NHOM 9: SECURITY (audit_log + login_attempt + date tables)
-- ============================================================

DROP TABLE IF EXISTS "audit_log" CASCADE;
CREATE TABLE "audit_log" (
    "log_id"        BIGSERIAL     PRIMARY KEY,
    "log_cat"       VARCHAR(50)   NOT NULL,
    "log_level"     VARCHAR(50)   NOT NULL DEFAULT 'INFO',
    "log_msg"       VARCHAR(500)  NOT NULL,
    "log_acc"       INTEGER,
    "log_name"      VARCHAR(100)  NOT NULL DEFAULT '',
    "log_ip"        VARCHAR(45)   NOT NULL DEFAULT '',
    "log_detail"    VARCHAR(1000) NOT NULL DEFAULT ''
);

DROP TABLE IF EXISTS "audit_date" CASCADE;
CREATE TABLE "audit_date" (
    "logd_id"       BIGSERIAL     PRIMARY KEY,
    "logd_log"      BIGINT        NOT NULL,
    "logd_at"       TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

DROP TABLE IF EXISTS "login_attempt" CASCADE;
CREATE TABLE "login_attempt" (
    "la_id"         SERIAL        PRIMARY KEY,
    "la_key"        VARCHAR(200)  NOT NULL UNIQUE,
    "la_fail"       INTEGER       NOT NULL DEFAULT 0,
    "la_locked"     BOOLEAN       NOT NULL DEFAULT FALSE
);

DROP TABLE IF EXISTS "login_date" CASCADE;
CREATE TABLE "login_date" (
    "lad_id"        SERIAL        PRIMARY KEY,
    "lad_la"        INTEGER       NOT NULL,
    "lad_last"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "lad_until"     TIMESTAMPTZ
);

-- ============================================================
-- BANG THAM CHIEU
-- ============================================================
DROP TABLE IF EXISTS "schema_map" CASCADE;
CREATE TABLE "schema_map" (
    "map_id"        SERIAL        PRIMARY KEY,
    "old_table"     VARCHAR(100)  NOT NULL,
    "old_column"    VARCHAR(100)  NOT NULL,
    "new_table"     VARCHAR(100)  NOT NULL,
    "new_column"    VARCHAR(100)  NOT NULL,
    "note"          VARCHAR(500)
);

INSERT INTO "schema_map" ("old_table","old_column","new_table","new_column","note") VALUES
('Users','UserId',    'account','acc_id',     'PK'),
('Users','UserName',  'account','acc_name',   'ten hien thi'),
('Users','MK',        'account','acc_hash',   'BCrypt hash mat khau'),
('Users','EMAIL',     'account','acc_email',  'AES-256-GCM encrypted'),
('Users','ROLE',      'account','acc_role',   'Admin|User|User VIP'),
('Users','IsLocked',  'account','acc_locked', 'khoa tai khoan'),
('Users','SecurityStamp','account','acc_stamp','logout all devices'),
('Users','CreatedAt', 'account_date','accd_created','ngay tao'),
('Users','Phone',        'profile','prof_phone',  'AES-256-GCM encrypted'),
('Users','Address',      'profile','prof_address','AES-256-GCM encrypted'),
('Users','Avatar',       'profile','prof_avatar', 'URL anh dai dien'),
('Users','VIPExpiryDate','profile_date','profd_vip_exp','ngay het han VIP'),
('Users','BalanceEncrypted','wallet','wal_balance','AES-256-GCM encrypted'),
('Movies','CreatedAt',  'movie_date','movd_created',  'ngay tao'),
('Transactions','CreatedAt','tx_date','txdate_created','ngay tao'),
('Bills','CreatedAt',    'bill_date','billd_created','ngay tao'),
('Currencies','LastUpdated','currency_date','curd_updated','cap nhat cuoi'),
('Favorites','AddedAt',   'favorite_date','favd_added','ngay them'),
('WatchHistories','WatchedAt','watch_date','whd_at','thoi diem xem'),
('AuditLogs','Timestamp', 'audit_date','logd_at','thoi diem'),
('LoginAttempts','LastAttempt','login_date','lad_last','lan cuoi'),
('LoginAttempts','LockedUntil','login_date','lad_until','khoa den khi');

-- ============================================================
-- DATA
-- ============================================================

INSERT INTO "account" ("acc_id","acc_name","acc_hash","acc_email","acc_role","acc_locked","acc_stamp") VALUES
(2,'Administrator','$2a$12$CQW/.RuOrUtCKDbh4w9RkuLg3GvxUsOBwfQPHnvMNJjzcpWeH9MF2','admin@webxemphim.com','Admin',FALSE,0),
(3,'NGUYEN DAT','$2a$12$bwfd.h0lVy6Qoj7QvBfLteOpEccXgt867kTRDSLBpB7nmVb486Wom','123@gmail.com','User',FALSE,0),
(4,'1 1','$2a$12$lSMCxWoFJhZ2fkQPNkNm9udVzOmWjQsDW/X8oLaPXKtWqW.Bi1orC','1@gmail.com','User',FALSE,0),
(5,'1 2','$2a$12$QQBfCLHrPDIZTZOCzwdfZOf.hO5A4Ki5QHv2iCtMMNDovDYPwpyYy','12@gmail.com','User',FALSE,0),
(6,'admin2','$2a$12$Pl6B/ctOvYDvB1RcQ5Ln4uR8FC6e4SftVxoNv1b9.Rh24EcJFYToi','ZP+OzfNruB4D918buDRiExsnGeEYmvcwCAp9r050gDtocQL4gcLF/i3ol+rqTcpv9g==','Admin',FALSE,0),
(7,'a 1','$2a$12$26nU7giUp/qhF8By3rNkT.rna3U54ZwCiGoIAuCimACV8IEa9FA2y','w0CzMUQsFwN6EMf7/fXTEiamxpCgWizb5QFdU5TLIT/hiJq9hG4dpsLA6d63A8ve16rp','User',FALSE,0),
(8,'Nguyễn Phước','$2a$12$H4fuTgJnlwFAOuoMFf4XFeMWfleMZTgiJUpw4fEm653X9joleosbS','X0LtGwb9Ed5gTR94yDdPKu32pb9CvrX9HihsHcHMS2zIeVVrMRYQTUTQJZ4lvxwQG5kD9KI=','User',FALSE,0);

INSERT INTO "account_date" ("accd_acc","accd_created") VALUES
(2,'2026-06-18T20:18:10.440643+00:00'),
(3,'2026-06-18T20:27:07.935436+00:00'),
(4,'2026-06-18T22:39:57.115101+00:00'),
(5,'2026-06-19T01:55:19.175148+00:00'),
(6,'2026-06-19T02:51:58.297311+00:00'),
(7,'2026-06-19T05:04:52.744164+00:00'),
(8,'2026-06-19T07:57:33.259009+00:00');

INSERT INTO "profile" ("prof_acc","prof_name") VALUES
(2,'Administrator'),(3,'NGUYEN DAT'),(4,'1 1'),(5,'1 2'),(6,'admin2'),(7,'a 1'),(8,'Nguyễn Phước');

INSERT INTO "profile_date" ("profd_prof","profd_vip_exp","profd_updated")
SELECT "prof_id", NULL, NOW() FROM "profile";

INSERT INTO "wallet" ("wal_acc","wal_name","wal_balance") VALUES
(2,'Administrator',''),
(3,'NGUYEN DAT',''),
(4,'1 1',''),
(5,'1 2',''),
(6,'admin2','FcbaPqZwVoLZuRvmkPP18X3Q2RCcg9t61ex3Cpc='),
(7,'a 1','56hxQs9g5ucguDMxa5ok5TYBaLtKEPUqgPR5nJ0='),
(8,'Nguyễn Phước','kkhU6ZbYip8M/zosF0lVPvPLG/JMuDBioMchW7Q=');

INSERT INTO "wallet_date" ("wald_wal","wald_updated")
SELECT "wal_id", NOW() FROM "wallet";

INSERT INTO "movie_info" ("mov_id","mov_title","mov_desc","mov_genre","mov_country","mov_year","mov_category","mov_vip","mov_active","mov_views") VALUES
(1,'Tazan Nhí: Cuộc Phiêu Lưu Kỳ Thú','Tazan Nhí là một cậu bé dũng cảm sống trong rừng rậm.','Phim Bộ','Việt Nam','2024','',FALSE,TRUE,0),
(2,'Thám Tử Conan: Tập 1','Kudo Shinichi là một thám tử trung học nổi tiếng.','Phim Bộ','Nhật Bản','1996','',FALSE,TRUE,0),
(3,'One Piece: Tập 1 - Tôi là Luffy','Monkey D. Luffy là một cậu bé mơ ước trở thành Vua Hải Tặc.','Phim Bộ','Nhật Bản','1999','',FALSE,TRUE,0),
(34,'vtv','ádasdsa','Chính trị','Việt Nam','2026','THỂ LOẠI',FALSE,TRUE,0),
(35,'đâs','đâs','Chính trị','Việt Nam','2024','Phim Bộ',FALSE,TRUE,0);

INSERT INTO "movie_date" ("movd_mov","movd_created") VALUES
(1,'0001-01-01T00:00:00+00:00'),(2,'0001-01-01T00:00:00+00:00'),(3,'0001-01-01T00:00:00+00:00'),
(34,'2026-06-19T07:57:37.695446+00:00'),(35,'2026-06-19T08:09:12.408403+00:00');

INSERT INTO "movie_media" ("media_mov","media_title","media_image","media_video") VALUES
(1,'Tazan Nhí: Cuộc Phiêu Lưu Kỳ Thú','/images/tazannhi/hqdefault.jpg','/videos/tazannhi/tazannhitap1.mp4'),
(2,'Thám Tử Conan: Tập 1','/images/conan/anh-conan-ngau.jpg','/videos/conan/conan-ep1.mp4'),
(3,'One Piece: Tập 1 - Tôi là Luffy','/images/conan/images.webp','/videos/conan/conan-ep1.mp4'),
(34,'vtv','1xR8qnxEe4IKYYoFjDi0ZiQVVk+WelmwJmAklUik/qbRmEBxWtlzfzO/LM8jzxGIZHTaDWOE8ggjctMfOXsLg99ZYQYQgBV9EUz8lpowzCZlE8k7','3yR3//NhZn5sQMgzSvBk+m1bvMkhGuN/CXQ7NVmd2odzjkux88rEWl6DqEG6IhYW2UGj3PCvbJVsV3k0c5lUXsErI8OO5pV8InzG4EHuSOOw/+l4'),
(35,'đâs','6wnsIyR+a0awjUekDPTToD0Bp2B0YUcfgz8owQcIFaG9BrGSPEU28hlTcMXa0tcVZnbK7E+BahsJttrD192IS4xl0xE1baf0DWjVRw5uV6d2sg8g','igA+Ds3aAuIYIQiMZxP7+pf1PNjPXoRTmSdQSAibhBUg84imFVkYQ5lGHKpa7+UW0mnqNHnwwEz9ZsDBc5jxPhQdN0iMKItLOlA1xgclk7juQI9u');

INSERT INTO "media_date" ("mediad_media","mediad_updated")
SELECT "media_id", NOW() FROM "movie_media";

INSERT INTO "category" ("cat_id","cat_name","cat_type","cat_active","cat_order") VALUES
(1,'CHỦ ĐỀ','CHỦ ĐỀ',TRUE,1),
(2,'THỂ LOẠI','THỂ LOẠI',TRUE,2),
(3,'Phim Lẻ','Phim Lẻ',TRUE,3),
(4,'Phim Bộ','Phim Bộ',TRUE,4);

INSERT INTO "login_attempt" ("la_id","la_key","la_fail","la_locked") VALUES
(1,'::ffff:100.64.0.7:123@gmail.com',0,FALSE),
(2,'::ffff:100.64.0.12:123@gmail.com',1,FALSE),
(3,'::ffff:100.64.0.4:123@gmail.com',0,FALSE),
(4,'::ffff:100.64.0.10:123@gmail.com',1,FALSE),
(5,'::ffff:100.64.0.4:admin',1,FALSE);

INSERT INTO "login_date" ("lad_la","lad_last","lad_until") VALUES
(1,'2026-06-19T06:35:10.180287+00:00',NULL),
(2,'2026-06-19T06:34:40.758804+00:00',NULL),
(3,'2026-06-19T06:35:26.569463+00:00',NULL),
(4,'2026-06-19T06:35:17.902274+00:00',NULL),
(5,'2026-06-19T07:56:23.944473+00:00',NULL);

INSERT INTO "audit_log" ("log_id","log_cat","log_level","log_msg","log_acc","log_name","log_ip","log_detail") VALUES
(1,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.8',''),
(2,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.4',''),
(3,'REGISTER','INFO','Dang ky moi: admin2',6,'admin2','::ffff:100.64.0.2',''),
(4,'REGISTER','INFO','Dang ky moi: a 1',7,'a 1','::ffff:100.64.0.16',''),
(5,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: a 1',7,'a 1','::ffff:100.64.0.16',''),
(6,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.3',''),
(7,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: NGUYEN DAT',3,'NGUYEN DAT','::ffff:100.64.0.8',''),
(8,'LOGOUT','INFO','Dang xuat: NGUYEN DAT',3,'NGUYEN DAT','::ffff:100.64.0.6',''),
(9,'LOGIN_FAIL','WARNING','Dang nhap that bai: 123@gmail.com',NULL,'123@gmail.com','::ffff:100.64.0.7',''),
(10,'LOGIN_FAIL','WARNING','Dang nhap that bai: 123@gmail.com',NULL,'123@gmail.com','::ffff:100.64.0.12',''),
(11,'LOGIN_FAIL','WARNING','Dang nhap that bai: 123@gmail.com',NULL,'123@gmail.com','::ffff:100.64.0.4',''),
(12,'LOGIN_FAIL','WARNING','Dang nhap that bai: 123@gmail.com',NULL,'123@gmail.com','::ffff:100.64.0.7',''),
(13,'LOGIN_FAIL','WARNING','Dang nhap that bai: 123@gmail.com',NULL,'123@gmail.com','::ffff:100.64.0.10',''),
(14,'LOGIN_FAIL','WARNING','Dang nhap that bai: 123@gmail.com',NULL,'123@gmail.com','::ffff:100.64.0.4',''),
(15,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.5',''),
(16,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: NGUYEN DAT',3,'NGUYEN DAT','::ffff:100.64.0.7',''),
(17,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.7',''),
(18,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: NGUYEN DAT',3,'NGUYEN DAT','::ffff:100.64.0.5',''),
(19,'LOGIN_FAIL','WARNING','Dang nhap that bai: admin',NULL,'admin','::ffff:100.64.0.4',''),
(20,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.2',''),
(21,'REGISTER','INFO','Dang ky moi: Nguyễn Phước',8,'Nguyễn Phước','::ffff:100.64.0.2',''),
(22,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Nguyễn Phước',8,'Nguyễn Phước','::ffff:100.64.0.11',''),
(23,'LOGOUT','INFO','Dang xuat: Administrator',2,'Administrator','::ffff:100.64.0.9',''),
(24,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.3',''),
(25,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: NGUYEN DAT',3,'NGUYEN DAT','::ffff:100.64.0.4',''),
(26,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.8',''),
(27,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: Administrator',2,'Administrator','::ffff:100.64.0.6',''),
(28,'LOGIN_OK','SUCCESS','Dang nhap thanh cong: NGUYEN DAT',3,'NGUYEN DAT','::ffff:100.64.0.4','');

INSERT INTO "audit_date" ("logd_log","logd_at") VALUES
(1,'2026-06-19T02:43:30.940397+00:00'),(2,'2026-06-19T02:51:00.683556+00:00'),(3,'2026-06-19T02:51:58.317875+00:00'),
(4,'2026-06-19T05:04:52.751776+00:00'),(5,'2026-06-19T05:05:06.623515+00:00'),(6,'2026-06-19T06:31:55.595216+00:00'),
(7,'2026-06-19T06:34:12.411525+00:00'),(8,'2026-06-19T06:34:21.094979+00:00'),(9,'2026-06-19T06:34:31.607616+00:00'),
(10,'2026-06-19T06:34:40.764764+00:00'),(11,'2026-06-19T06:35:00.802489+00:00'),(12,'2026-06-19T06:35:10.189677+00:00'),
(13,'2026-06-19T06:35:17.907605+00:00'),(14,'2026-06-19T06:35:26.573569+00:00'),(15,'2026-06-19T06:37:22.234286+00:00'),
(16,'2026-06-19T06:37:47.428567+00:00'),(17,'2026-06-19T07:33:57.851747+00:00'),(18,'2026-06-19T07:35:37.668723+00:00'),
(19,'2026-06-19T07:56:24.010395+00:00'),(20,'2026-06-19T07:56:34.341893+00:00'),(21,'2026-06-19T07:57:33.267754+00:00'),
(22,'2026-06-19T07:57:43.991204+00:00'),(23,'2026-06-19T07:59:25.539036+00:00'),(24,'2026-06-19T07:59:36.945573+00:00'),
(25,'2026-06-19T08:01:05.102617+00:00'),(26,'2026-06-19T08:13:37.098569+00:00'),(27,'2026-06-19T08:20:46.212724+00:00'),
(28,'2026-06-19T08:31:11.357415+00:00');

-- ============================================================
-- SEQUENCE RESET
-- ============================================================
SELECT setval(pg_get_serial_sequence('"account"',       'acc_id'),      COALESCE((SELECT MAX(acc_id)      FROM "account"),       1), true);
SELECT setval(pg_get_serial_sequence('"profile"',       'prof_id'),     COALESCE((SELECT MAX(prof_id)     FROM "profile"),      1), true);
SELECT setval(pg_get_serial_sequence('"wallet"',        'wal_id'),      COALESCE((SELECT MAX(wal_id)      FROM "wallet"),       1), true);
SELECT setval(pg_get_serial_sequence('"movie_info"',    'mov_id'),      COALESCE((SELECT MAX(mov_id)      FROM "movie_info"),   1), true);
SELECT setval(pg_get_serial_sequence('"movie_media"',   'media_id'),    COALESCE((SELECT MAX(media_id)    FROM "movie_media"),  1), true);
SELECT setval(pg_get_serial_sequence('"category"',      'cat_id'),      COALESCE((SELECT MAX(cat_id)      FROM "category"),     1), true);
SELECT setval(pg_get_serial_sequence('"login_attempt"', 'la_id'),       COALESCE((SELECT MAX(la_id)       FROM "login_attempt"),1), true);
SELECT setval(pg_get_serial_sequence('"audit_log"',     'log_id'),      COALESCE((SELECT MAX(log_id)      FROM "audit_log"),    1), true);

-- ============================================================
-- HOAN THANH - 27 bang (15 data + 12 date)
-- ============================================================
