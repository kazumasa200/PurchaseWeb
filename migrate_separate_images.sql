-- ============================================================
-- Migration: product.image_base64 を product_images テーブルへ分離
-- 実行方法: psql -d <your_db> -f migrate_separate_images.sql
-- ============================================================

-- 1. product_images テーブルを作成
CREATE TABLE IF NOT EXISTS product_images (
    image_id    VARCHAR(100) PRIMARY KEY,
    product_id  VARCHAR(100) NOT NULL,
    image_base64 TEXT,
    created_at  TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_product_images_product
        FOREIGN KEY (product_id) REFERENCES product(product_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_product_images_product_id
    ON product_images(product_id);

-- 2. 既存データを移行（image_base64 が入っている商品のみ）
INSERT INTO product_images (image_id, product_id, image_base64, created_at)
SELECT
    gen_random_uuid()::text,
    product_id,
    image_base64,
    NOW()
FROM product
WHERE image_base64 IS NOT NULL AND image_base64 != '';

-- 3. product テーブルから image_base64 カラムを削除
ALTER TABLE product DROP COLUMN IF EXISTS image_base64;

-- ============================================================
-- 確認クエリ
-- SELECT COUNT(*) FROM product_images;
-- SELECT column_name FROM information_schema.columns
--   WHERE table_name = 'product';
-- ============================================================
