import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, Inject, ViewChild } from '@angular/core';
import * as pbi from 'powerbi-client';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  @ViewChild('reportContainer', { static: false }) reportContainer: ElementRef;
  report: pbi.Report;
  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    http.get<any>(baseUrl + 'user/power-bi-token').subscribe(result => {
      console.log(result);
      this.report = this.getReport(result.embedConfig, this.reportContainer.nativeElement);
    }, error => console.error(error));
  }

  getReport(request: any, reportContainer: any): any {
    const config: pbi.IEmbedConfiguration = {
      type: 'report',
      tokenType: pbi.models.TokenType.Embed,
      id: request.id,
      embedUrl: request.embedUrl,
      accessToken: request.embedToken.token,
      settings: { filterPaneEnabled: false }
    };
    const powerbi = new pbi.service.Service(pbi.factories.hpmFactory, pbi.factories.wpmpFactory, pbi.factories.routerFactory);
    return <pbi.Report>powerbi.embed(reportContainer, config);
  }
}

