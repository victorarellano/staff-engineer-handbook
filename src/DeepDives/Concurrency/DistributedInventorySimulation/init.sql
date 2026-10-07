CREATE TABLE IF NOT EXISTS product_inventory
(
    product_id INTEGER PRIMARY KEY,
    available_stock INTEGER NOT NULL CHECK (available_stock >= 0)
);

INSERT INTO product_inventory (product_id, available_stock) VALUES (1, 2)
ON CONFLICT (product_id)
DO UPDATE SET available_stock = EXCLUDED.available_stock;