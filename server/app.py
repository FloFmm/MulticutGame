from flask import Flask, request, jsonify
from flask_cors import CORS
import psycopg2
import os 
from psycopg2.extras import RealDictCursor

app = Flask(__name__)
CORS(app)

DB_PARAMS = {
    'dbname': 'testdb',
    'user': 'testuser',
    'password': 'testpass',
    'host': 'localhost',  # e.g., 'localhost' or Render DB hostname
    'port': 5432,         # default PostgreSQL port
}
# DB_PARAMS = {
#     'dbname': os.getenv('DB_NAME'),
#     'user': os.getenv('DB_USER'),
#     'password': os.getenv('DB_PASSWORD'),
#     'host': os.getenv('DB_HOST'),
#     'port': int(os.getenv('DB_PORT', 5432)),
# }

def get_conn():
    return psycopg2.connect(**DB_PARAMS)

@app.route('/', methods=['GET'])
def home():
    return jsonify({'message': 'Welcome to the Highscores API'})

@app.route('/submit', methods=['POST'])
def submit_levels():
    data = request.get_json()
    player_id = data.get('player_id')
    player_name = data.get('player_name')
    graphs = data.get('graphs')

    if not player_id or not player_name or not isinstance(graphs, list):
        return jsonify({'status': 'error', 'message': 'Invalid data'}), 400

    try:
        conn = get_conn()
        cur = conn.cursor()

        # Upsert player
        cur.execute('''
            INSERT INTO players (id, name)
            VALUES (%s, %s)
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name
        ''', (player_id, player_name))

        # Insert completed levels
        for graph in graphs:
            level_id = graph.get('Name')
            level_name = graph.get('Name')  # You can change this if ID ≠ Name

            if level_id is None:
                continue

            # Ensure level exists in 'levels' table
            cur.execute('''
                INSERT INTO levels (id, name)
                VALUES (%s, %s)
                ON CONFLICT (id) DO NOTHING
            ''', (level_id, level_name))

            # Insert if level completed optimally
            if graph.get('OptimalCost') == graph.get('BestAchievedCost'):
                cur.execute('''
                    INSERT INTO player_levels_done (player_id, level_id)
                    VALUES (%s, %s)
                    ON CONFLICT DO NOTHING
                ''', (player_id, level_id))

        conn.commit()
        cur.close()
        conn.close()

        return jsonify({'status': 'success'}), 200

    except Exception as e:
        return jsonify({'status': 'error', 'message': str(e)}), 500

@app.route('/leaderboard/<player_id>', methods=['GET'])
def leaderboard(player_id):
    try:
        conn = get_conn()
        cur = conn.cursor(cursor_factory=RealDictCursor)

        # Get all players and their completed level counts
        cur.execute('''
            SELECT p.id AS player_id, p.name, COUNT(pl.level_id) AS levels_done
            FROM players p
            LEFT JOIN player_levels_done pl ON p.id = pl.player_id
            GROUP BY p.id, p.name
            ORDER BY levels_done DESC
        ''')
        all_players = cur.fetchall()

        # Find index of current player
        target_index = next((i for i, row in enumerate(all_players) if row['player_id'] == player_id), None)

        if target_index is None:
            return jsonify({'status': 'error', 'message': 'Player not found'}), 404

        top_100 = all_players[:100]
        local_slice = all_players[max(0, target_index - 10): target_index + 11]

        return jsonify({
            'top_100': top_100,
            'local_slice': local_slice
        })

    except Exception as e:
        return jsonify({'status': 'error', 'message': str(e)}), 500


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
