import sqlite3
import urllib.parse

conn = sqlite3.connect('racing_telemetry.db')
c = conn.cursor()

c.execute("SELECT TrackId, Name, ImagePath FROM Tracks")
tracks = c.fetchall()

# group by uppercase name
groups = {}
for t in tracks:
    tid, name, path = t
    uname = name.upper()
    if uname not in groups:
        groups[uname] = []
    groups[uname].append(t)

for uname, group in groups.items():
    if len(group) > 1:
        # Keep the first one
        keep_id = min([g[0] for g in group])
        for g in group:
            if g[0] != keep_id:
                # delete sessions first
                c.execute("DELETE FROM Sessions WHERE TrackId = ?", (g[0],))
                # delete track
                c.execute("DELETE FROM Tracks WHERE TrackId = ?", (g[0],))
                print(f"Deleted duplicate track {g[1]} ID {g[0]}")

# Update image paths
updates = {
    "LE MANS": "/images/tracks/LeMans.jpg",
    "SPA-FRANCORCHAMPS": "/images/tracks/Spa Francochamps.jpg",
    "MONZA": "/images/tracks/Monza.jpg",
    "SUZUKA": "/images/tracks/Suzuka.jpg",
    "NURBURGRING": "/images/tracks/Nurburging.jpg",
    "SILVERSTONE": "/images/tracks/Silverstone.jpg"
}

for uname, img in updates.items():
    c.execute("UPDATE Tracks SET ImagePath = ? WHERE UPPER(Name) = ?", (img, uname))
    print(f"Updated image for {uname} to {img}")

conn.commit()
conn.close()
