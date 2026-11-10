using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PollingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPollsToSurveys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                LOCK TABLE polls, poll_options, votes IN ACCESS EXCLUSIVE MODE;
                """);

            migrationBuilder.CreateTable(
                name: "surveys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "draft"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surveys", x => x.id);
                    table.CheckConstraint("ck_surveys_closed_at", "closed_at IS NULL OR closed_at >= created_at");
                    table.CheckConstraint("ck_surveys_ends_at", "ends_at IS NULL OR ends_at > created_at");
                    table.CheckConstraint("ck_surveys_status", "status IN ('draft', 'active', 'closed')");
                    table.ForeignKey(
                        name: "fk_surveys_author",
                        column: x => x.author_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questions", x => x.id);
                    table.UniqueConstraint("ak_questions_id_survey_id", x => new { x.id, x.survey_id });
                    table.CheckConstraint("ck_questions_position", "position >= 0");
                    table.CheckConstraint("ck_questions_type", "type IN ('single_choice', 'multiple_choice', 'text')");
                    table.ForeignKey(
                        name: "fk_questions_survey",
                        column: x => x.survey_id,
                        principalTable: "surveys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_survey_responses", x => x.id);
                    table.UniqueConstraint("ak_survey_responses_id_survey_id", x => new { x.id, x.survey_id });
                    table.ForeignKey(
                        name: "fk_survey_responses_survey",
                        column: x => x.survey_id,
                        principalTable: "surveys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_survey_responses_user",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_options", x => x.id);
                    table.UniqueConstraint("ak_question_options_id_question_id", x => new { x.id, x.question_id });
                    table.CheckConstraint("ck_question_options_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_question_options_question",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answers", x => x.id);
                    table.UniqueConstraint("ak_answers_id_question_id", x => new { x.id, x.question_id });
                    table.CheckConstraint("ck_answers_text_value", "text_value IS NULL OR btrim(text_value) <> ''");
                    table.ForeignKey(
                        name: "fk_answers_question_survey",
                        columns: x => new { x.question_id, x.survey_id },
                        principalTable: "questions",
                        principalColumns: new[] { "id", "survey_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_answers_response_survey",
                        columns: x => new { x.response_id, x.survey_id },
                        principalTable: "survey_responses",
                        principalColumns: new[] { "id", "survey_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "answer_selected_options",
                columns: table => new
                {
                    answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answer_selected_options", x => new { x.answer_id, x.option_id });
                    table.ForeignKey(
                        name: "fk_answer_selected_options_answer_question",
                        columns: x => new { x.answer_id, x.question_id },
                        principalTable: "answers",
                        principalColumns: new[] { "id", "question_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_answer_selected_options_option_question",
                        columns: x => new { x.option_id, x.question_id },
                        principalTable: "question_options",
                        principalColumns: new[] { "id", "question_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_answer_selected_options_option",
                table: "answer_selected_options",
                column: "option_id");

            migrationBuilder.CreateIndex(
                name: "ix_answers_question_survey",
                table: "answers",
                columns: new[] { "question_id", "survey_id" });

            migrationBuilder.CreateIndex(
                name: "ux_answers_response_question",
                table: "answers",
                columns: new[] { "response_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_question_options_question_position",
                table: "question_options",
                columns: new[] { "question_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_questions_survey_position",
                table: "questions",
                columns: new[] { "survey_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_survey_responses_user_submitted_at",
                table: "survey_responses",
                columns: new[] { "user_id", "submitted_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_survey_responses_survey_user",
                table: "survey_responses",
                columns: new[] { "survey_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_surveys_author_created_at",
                table: "surveys",
                columns: new[] { "author_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_surveys_status_created_at",
                table: "surveys",
                columns: new[] { "status", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_surveys_status_ends_at",
                table: "surveys",
                columns: new[] { "status", "ends_at" });
            // Copy and verify every value before removing the old tables, in the same transaction.
            migrationBuilder.Sql("""
                INSERT INTO surveys (id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at) SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM polls;
                -- IDs are unique within each table: reuse poll IDs for the single legacy question.
                INSERT INTO questions (id, survey_id, title, type, is_required, position)
                SELECT id, id, title, 'single_choice', true, 0 FROM polls;
                INSERT INTO question_options (id, question_id, text, position)
                SELECT id, poll_id, text, position FROM poll_options;
                INSERT INTO survey_responses (id, survey_id, user_id, submitted_at)
                SELECT id, poll_id, user_id, created_at FROM votes;
                -- Reusing vote IDs preserves traceability without extensions or mapping tables.
                INSERT INTO answers (id, response_id, survey_id, question_id, text_value)
                SELECT id, id, poll_id, poll_id, NULL FROM votes;
                INSERT INTO answer_selected_options (answer_id, question_id, option_id)
                SELECT id, poll_id, option_id FROM votes;
                """);

            migrationBuilder.Sql("""
                DO $verify$
                BEGIN
                    IF EXISTS ((SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM polls EXCEPT SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM surveys) UNION ALL (SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM surveys EXCEPT SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM polls)) THEN
                        RAISE EXCEPTION 'Data verification failed: surveys';
                    END IF;
                    IF EXISTS ((SELECT id, id, title, 'single_choice', true, 0 FROM polls EXCEPT SELECT id, survey_id, title, type, is_required, position FROM questions) UNION ALL (SELECT id, survey_id, title, type, is_required, position FROM questions EXCEPT SELECT id, id, title, 'single_choice', true, 0 FROM polls)) THEN
                        RAISE EXCEPTION 'Data verification failed: questions';
                    END IF;
                    IF EXISTS ((SELECT id, poll_id, text, position FROM poll_options EXCEPT SELECT id, question_id, text, position FROM question_options) UNION ALL (SELECT id, question_id, text, position FROM question_options EXCEPT SELECT id, poll_id, text, position FROM poll_options)) THEN
                        RAISE EXCEPTION 'Data verification failed: options';
                    END IF;
                    IF EXISTS ((SELECT id, poll_id, user_id, created_at FROM votes EXCEPT SELECT id, survey_id, user_id, submitted_at FROM survey_responses) UNION ALL (SELECT id, survey_id, user_id, submitted_at FROM survey_responses EXCEPT SELECT id, poll_id, user_id, created_at FROM votes)) THEN
                        RAISE EXCEPTION 'Data verification failed: responses';
                    END IF;
                    IF EXISTS ((SELECT id, id, poll_id, poll_id, NULL::text FROM votes EXCEPT SELECT id, response_id, survey_id, question_id, text_value FROM answers) UNION ALL (SELECT id, response_id, survey_id, question_id, text_value FROM answers EXCEPT SELECT id, id, poll_id, poll_id, NULL::text FROM votes)) THEN
                        RAISE EXCEPTION 'Data verification failed: answers';
                    END IF;
                    IF EXISTS ((SELECT id, poll_id, option_id FROM votes EXCEPT SELECT answer_id, question_id, option_id FROM answer_selected_options) UNION ALL (SELECT answer_id, question_id, option_id FROM answer_selected_options EXCEPT SELECT id, poll_id, option_id FROM votes)) THEN
                        RAISE EXCEPTION 'Data verification failed: selections';
                    END IF;
                END $verify$;
                """);

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "poll_options");

            migrationBuilder.DropTable(
                name: "polls");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                LOCK TABLE surveys, questions, question_options, survey_responses, answers, answer_selected_options
                IN ACCESS EXCLUSIVE MODE;
                DO $guard$
                BEGIN
                    IF EXISTS (SELECT 1 FROM surveys s
                               WHERE s.status = 'draft'
                                  OR (SELECT count(*) FROM questions q WHERE q.survey_id = s.id) <> 1)
                       OR EXISTS (SELECT 1 FROM questions q JOIN surveys s ON s.id = q.survey_id
                                  WHERE q.id <> s.id OR q.title <> s.title OR q.type <> 'single_choice'
                                     OR NOT q.is_required OR q.position <> 0)
                       OR EXISTS (SELECT 1 FROM survey_responses r
                                  WHERE (SELECT count(*) FROM answers a WHERE a.response_id = r.id) <> 1)
                       OR EXISTS (SELECT 1 FROM answers a
                                  WHERE a.id <> a.response_id OR a.text_value IS NOT NULL
                                     OR (SELECT count(*) FROM answer_selected_options o WHERE o.answer_id = a.id) <> 1)
                    THEN
                        RAISE EXCEPTION 'Cannot revert Surveys to Polls without data loss. Only legacy-compatible single-question surveys can be reverted.';
                    END IF;
                END $guard$;
                """);

            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    description = table.Column<string>(type: "text", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "active"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_polls", x => x.id);
                    table.CheckConstraint("ck_polls_closed_at", "closed_at IS NULL OR closed_at >= created_at");
                    table.CheckConstraint("ck_polls_ends_at", "ends_at IS NULL OR ends_at > created_at");
                    table.CheckConstraint("ck_polls_status", "status IN ('active', 'closed')");
                    table.ForeignKey(
                        name: "fk_polls_author",
                        column: x => x.author_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poll_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    poll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_poll_options", x => x.id);
                    table.UniqueConstraint("ak_poll_options_id_poll_id", x => new { x.id, x.poll_id });
                    table.CheckConstraint("ck_poll_options_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_poll_options_poll",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    poll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_votes", x => x.id);
                    table.ForeignKey(
                        name: "fk_votes_option_poll",
                        columns: x => new { x.option_id, x.poll_id },
                        principalTable: "poll_options",
                        principalColumns: new[] { "id", "poll_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_votes_poll",
                        column: x => x.poll_id,
                        principalTable: "polls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_votes_user",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_poll_options_poll_position",
                table: "poll_options",
                columns: new[] { "poll_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_polls_author_created_at",
                table: "polls",
                columns: new[] { "author_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_polls_status_created_at",
                table: "polls",
                columns: new[] { "status", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_polls_status_ends_at",
                table: "polls",
                columns: new[] { "status", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "ix_votes_option_poll",
                table: "votes",
                columns: new[] { "option_id", "poll_id" });

            migrationBuilder.CreateIndex(
                name: "ix_votes_poll_option",
                table: "votes",
                columns: new[] { "poll_id", "option_id" });

            migrationBuilder.CreateIndex(
                name: "ix_votes_user",
                table: "votes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_votes_poll_user",
                table: "votes",
                columns: new[] { "poll_id", "user_id" },
                unique: true);
            migrationBuilder.Sql("""
                INSERT INTO polls (id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at) SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM surveys;
                INSERT INTO poll_options (id, poll_id, text, position) SELECT o.id, q.survey_id, o.text, o.position FROM question_options o JOIN questions q ON q.id = o.question_id;
                INSERT INTO votes (id, poll_id, option_id, user_id, created_at) SELECT r.id, r.survey_id, o.option_id, r.user_id, r.submitted_at
                FROM survey_responses r JOIN answers a ON a.response_id = r.id
                JOIN answer_selected_options o ON o.answer_id = a.id;
                """);

            migrationBuilder.Sql("""
                DO $verify$
                BEGIN
                    IF EXISTS ((SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM surveys EXCEPT SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM polls) UNION ALL (SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM polls EXCEPT SELECT id, author_id, title, description, status, created_at, updated_at, ends_at, closed_at FROM surveys)) THEN
                        RAISE EXCEPTION 'Data verification failed: restored polls';
                    END IF;
                    IF EXISTS ((SELECT o.id, q.survey_id, o.text, o.position FROM question_options o JOIN questions q ON q.id = o.question_id EXCEPT SELECT id, poll_id, text, position FROM poll_options) UNION ALL (SELECT id, poll_id, text, position FROM poll_options EXCEPT SELECT o.id, q.survey_id, o.text, o.position FROM question_options o JOIN questions q ON q.id = o.question_id)) THEN
                        RAISE EXCEPTION 'Data verification failed: restored options';
                    END IF;
                    IF EXISTS ((SELECT r.id, r.survey_id, o.option_id, r.user_id, r.submitted_at
                FROM survey_responses r JOIN answers a ON a.response_id = r.id
                JOIN answer_selected_options o ON o.answer_id = a.id EXCEPT SELECT id, poll_id, option_id, user_id, created_at FROM votes) UNION ALL (SELECT id, poll_id, option_id, user_id, created_at FROM votes EXCEPT SELECT r.id, r.survey_id, o.option_id, r.user_id, r.submitted_at
                FROM survey_responses r JOIN answers a ON a.response_id = r.id
                JOIN answer_selected_options o ON o.answer_id = a.id)) THEN
                        RAISE EXCEPTION 'Data verification failed: restored votes';
                    END IF;
                END $verify$;
                """);

            migrationBuilder.DropTable(
                name: "answer_selected_options");

            migrationBuilder.DropTable(
                name: "answers");

            migrationBuilder.DropTable(
                name: "question_options");

            migrationBuilder.DropTable(
                name: "survey_responses");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "surveys");

        }
    }
}
