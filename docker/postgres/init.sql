-- One Postgres container hosts a database per service. Each service only ever
-- connects to its own database, keeping the database-per-service boundary real.
CREATE DATABASE orderflow_orders;
CREATE DATABASE orderflow_inventory;
