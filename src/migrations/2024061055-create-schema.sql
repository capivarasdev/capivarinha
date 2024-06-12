CREATE TABLE user_die_roll (
    user_id TEXT NOT NULL,
    rolled_at DATETIME NOT NULL,
    PRIMARY KEY (user_id)
);

CREATE TABLE user_die_mute (
    user_id TEXT NOT NULL,
    user_id_mute TEXT NOT NULL,
    muted_until DATETIME NOT NULL,
    PRIMARY KEY (user_id, user_id_mute)
);