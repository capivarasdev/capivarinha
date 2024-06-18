CREATE TABLE "user" (
	"id"	INTEGER NOT NULL,
	"discord_id"	TEXT NOT NULL UNIQUE,
	PRIMARY KEY("id" AUTOINCREMENT)
);

CREATE TABLE "transaction" (
	"id"	INTEGER NOT NULL,
	"from_user_id"	INTEGER NOT NULL CHECK("from_user_id" <> "to_user_id"),
	"to_user_id"	INTEGER NOT NULL,
	"amount"	INTEGER NOT NULL CHECK("amount" > 0),
	"transaction_date"	TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY("id" AUTOINCREMENT),
	FOREIGN KEY("to_user_id") REFERENCES "user"("id"),
	FOREIGN KEY("from_user_id") REFERENCES "user"("id")
);
