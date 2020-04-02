import { Component, OnInit } from '@angular/core';
import { AuthService } from './_services/auth.service';
import {JwtHelperService} from '@auth0/angular-jwt';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {

  jwtHelper = new JwtHelperService();

  constructor(private autheService: AuthService){
  }

  ngOnInit(): void {
    const token = localStorage.getItem(environment.tokenName);
    if (token){
      this.autheService.decodedToken = this.jwtHelper.decodeToken(token);
    }
  }
}
