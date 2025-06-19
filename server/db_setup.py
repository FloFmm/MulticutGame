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
            'dbname': "testdb", #os.getenv('DB_NAME'),
            'user': "testuser", #os.getenv('DB_USER'),
            'password':"testpass", #os.getenv('DB_PASSWORD'),
            'host': "localhost",#os.getenv('DB_HOST'),
            'port': 5432#int(os.getenv('DB_PORT', 5432)),
        }
    
def main():
    DB_PARAMS = get_db_params()
    conn = psycopg2.connect(**DB_PARAMS)
    conn.autocommit = True
    cur = conn.cursor()

    # Create table if not exists
    # cur.execute('''
    # CREATE TABLE IF NOT EXISTS highscores (
    #     id SERIAL PRIMARY KEY,
    #     player TEXT NOT NULL,
    #     score INTEGER NOT NULL
    # );
    # ''')
    cur.execute('''
        CREATE TABLE IF NOT EXISTS players (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS levels (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS player_levels_done (
            player_id TEXT REFERENCES players(id) ON DELETE CASCADE,
            level_id TEXT REFERENCES levels(id) ON DELETE CASCADE,
            PRIMARY KEY (player_id, level_id)
        );

        CREATE TABLE IF NOT EXISTS challenges (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS player_challenges (
            player_id TEXT REFERENCES players(id) ON DELETE CASCADE,
            challenge_id TEXT REFERENCES challenges(id) ON DELETE CASCADE,
            highscore INTEGER NOT NULL,
            time_taken FLOAT NOT NULL,
            PRIMARY KEY (player_id, challenge_id)
        );
    ''')


    cur.close()
    conn.close()

if __name__ == '__main__':
    main()