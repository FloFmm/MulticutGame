import psycopg2
import os
from psycopg2 import sql
from urllib.parse import urlparse
import sys

def parse_database_url(db_url):
    """
    Parse a Postgres database URL into connection params dict.
    Example URL: postgres://user:pass@host:port/dbname
    """
    result = urlparse(db_url)
    return {
        'dbname': result.path.lstrip('/'),
        'user': result.username,
        'password': result.password,
        'host': result.hostname,
        'port': result.port or 5432,
    }

def get_db_params():
    if len(sys.argv) > 1:
        db_url = sys.argv[1]
        print(f"Using DB URL from argument: {db_url}")
        return parse_database_url(db_url)
    else:
        print("No DB URL argument provided, falling back to environment variables.")
        return {
            'dbname': os.getenv('DB_NAME'),
            'user': os.getenv('DB_USER'),
            'password': os.getenv('DB_PASSWORD'),
            'host': os.getenv('DB_HOST'),
            'port': int(os.getenv('DB_PORT', 5432)),
        }
    
def main():
    DB_PARAMS = get_db_params()
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

if __name__ == '__main__':
    main()