import psycopg2
from psycopg2 import sql

# Update these to your PostgreSQL credentials:
DB_PARAMS = {
    'dbname': 'testdb',
    'user': 'testuser',
    'password': 'testpass',
    'host': 'localhost',  # e.g., 'localhost' or Render DB hostname
    'port': 5432,         # default PostgreSQL port
}

conn = psycopg2.connect(**DB_PARAMS)
conn.autocommit = True
cur = conn.cursor()

# Create table if not exists
cur.execute('''
CREATE TABLE IF NOT EXISTS highscores (
    id SERIAL PRIMARY KEY,
    player TEXT NOT NULL,
    score INTEGER NOT NULL
);
''')

cur.close()
conn.close()
