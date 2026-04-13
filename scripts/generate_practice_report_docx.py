from __future__ import annotations

import argparse
from datetime import date
from pathlib import Path
from xml.sax.saxutils import escape
from zipfile import ZipFile, ZIP_DEFLATED


def p(text: str) -> str:
    safe = escape(text)
    return (
        "<w:p><w:r><w:t xml:space=\"preserve\">"
        f"{safe}"
        "</w:t></w:r></w:p>"
    )


def build_document_xml(lines: list[str]) -> str:
    body = "".join(p(line) for line in lines)
    return f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:wpc="http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas"
 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
 xmlns:o="urn:schemas-microsoft-com:office:office"
 xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"
 xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math"
 xmlns:v="urn:schemas-microsoft-com:vml"
 xmlns:wp14="http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing"
 xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
 xmlns:w10="urn:schemas-microsoft-com:office:word"
 xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
 xmlns:w14="http://schemas.microsoft.com/office/word/2010/wordml"
 xmlns:wpg="http://schemas.microsoft.com/office/word/2010/wordprocessingGroup"
 xmlns:wpi="http://schemas.microsoft.com/office/word/2010/wordprocessingInk"
 xmlns:wne="http://schemas.microsoft.com/office/2006/wordml"
 xmlns:wps="http://schemas.microsoft.com/office/word/2010/wordprocessingShape"
 mc:Ignorable="w14 wp14">
 <w:body>
  {body}
  <w:sectPr>
   <w:pgSz w:w="11906" w:h="16838"/>
   <w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/>
  </w:sectPr>
 </w:body>
</w:document>
'''


def create_docx(out_path: Path, lines: list[str]) -> None:
    content_types = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
'''

    rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
'''

    document_rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>
'''

    today = date.today().isoformat()
    core = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
 xmlns:dc="http://purl.org/dc/elements/1.1/"
 xmlns:dcterms="http://purl.org/dc/terms/"
 xmlns:dcmitype="http://purl.org/dc/dcmitype/"
 xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>Технический отчет по практике PM.02/PM.04</dc:title>
  <dc:creator>TOApp</dc:creator>
  <cp:lastModifiedBy>TOApp</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{today}T00:00:00Z</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{today}T00:00:00Z</dcterms:modified>
</cp:coreProperties>
'''

    app = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"
 xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Microsoft Office Word</Application>
</Properties>
'''

    out_path.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(out_path, "w", ZIP_DEFLATED) as zf:
        zf.writestr("[Content_Types].xml", content_types)
        zf.writestr("_rels/.rels", rels)
        zf.writestr("word/document.xml", build_document_xml(lines))
        zf.writestr("word/_rels/document.xml.rels", document_rels)
        zf.writestr("docProps/core.xml", core)
        zf.writestr("docProps/app.xml", app)


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate PM.02/PM.04 practice report in .docx format.")
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("artifacts/Технический_отчет_практика_PM02_PM04.docx"),
        help="Output .docx path (default: artifacts/Технический_отчет_практика_PM02_PM04.docx)",
    )
    args = parser.parse_args()

    lines = [
        "ТЕХНИЧЕСКИЙ ОТЧЕТ ПО ПРОИЗВОДСТВЕННОЙ ПРАКТИКЕ",
        "ПМ.02 Осуществление интеграции программных модулей",
        "ПМ.04 Сопровождение и обслуживание программного обеспечения компьютерных систем",
        "",
        "1. ВВЕДЕНИЕ",
        "Место практики: разработка и сопровождение настольного приложения TOApp (WPF, C#, EF Core, SQL Server).",
        "Цель: получить практический опыт разработки, интеграции, сопровождения и обслуживания программного обеспечения.",
        "",
        "2. ОРГАНИЗАЦИЯ ОХРАНЫ ТРУДА И ТЕХНИКИ БЕЗОПАСНОСТИ",
        "Перед началом работ проведен инструктаж по ТБ: работа с ПК, электробезопасность, эргономика рабочего места, резервное копирование.",
        "",
        "3. ОПИСАНИЕ ВЫПОЛНЕННЫХ РАБОТ (по аттестационному листу)",
        "ПК 2.1 Анализ функциональных и эксплуатационных требований, проектирование структуры модулей и данных.",
        "ПК 2.2 Реализация и интеграция модулей: UI, ViewModel, слой доступа к данным, операции поступления/продажи, расчет остатков.",
        "ПК 2.3 Отладка модулей: проверка бизнес-логики команд, обработка ошибок, проверка сценариев с недостаточным остатком.",
        "ПК 2.4 Тестирование: подготовка тестовых сценариев (создание/изменение/удаление товара, проведение документов, фильтрация остатков).",
        "ПК 2.5 Инспектирование кода: проверка на соответствие принятым соглашениям и читаемости.",
        "ПК 4.1 Установка/настройка ПО: подготовка среды, подключение к SQL Server, первичная загрузка данных.",
        "ПК 4.2 Измерение эксплуатационных характеристик: оценка времени отклика операций загрузки/сохранения на типовом наборе данных.",
        "ПК 4.3 Модификация компонентов: обновление интерфейса, рефакторинг ресурсов стилей, поддержка пользовательских сценариев.",
        "ПК 4.4 Обеспечение защиты: рекомендации по переносу строки подключения в конфигурацию и ограничению прав учетной записи БД.",
        "",
        "4. РЕЗУЛЬТАТЫ",
        "— Реализован и поддерживается рабочий функционал учета: товары, справочники, поступление, продажа, остатки.",
        "— Выполнена интеграция с базой данных через Entity Framework Core.",
        "— Улучшен пользовательский интерфейс и унифицирована система стилей.",
        "",
        "5. ЗАКЛЮЧЕНИЕ",
        "В ходе практики получены навыки проектирования, интеграции, сопровождения и модернизации прикладного ПО.",
        "Требования ПМ.02 и ПМ.04 отражены в выполненных работах и могут быть подтверждены кодом, коммитами и демонстрацией приложения.",
        "",
        "6. ПРИЛОЖЕНИЯ",
        "1) Скриншоты интерфейса приложения.",
        "2) Фрагменты кода (ViewModel, XAML, DbContext).",
        "3) Дневник практики и аттестационные листы.",
    ]

    create_docx(args.output, lines)
    print(f"Created: {args.output}")


if __name__ == "__main__":
    main()
