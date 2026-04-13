# Документы отчета

Бинарные `.docx` файлы не хранятся в Git.

Сгенерировать Word-отчет:

```bash
python scripts/generate_practice_report_docx.py
```

По умолчанию файл создается в:

`artifacts/Технический_отчет_практика_PM02_PM04.docx`

Можно указать свой путь:

```bash
python scripts/generate_practice_report_docx.py --output docs/Технический_отчет_практика_PM02_PM04.docx
```
