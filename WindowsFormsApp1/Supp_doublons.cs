using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO.Compression;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;
using System.Diagnostics;

//=================================================================
//
//  Dev par Arnaud Gariel                                29/07/2022
//  arnaud.gariel@gmail.com
//  Programme permettant la suppresions des doublons dans les 
//  fichiers log. La structure des dossiers étudiés est la suivante:
//
//    >DATAs
//          >DATA
//              > dossier
//                 > {int}
//                      > {int}
//                          > probe.log
//                          > probe.1.log
//                          > ...
//                 > H
//                      > {int}
//                          > probe.log
//                          > probe.1.log
//                          > ...
//                 > NC1
//                      > mail
//                      > dossier_pj
//                          > pj
//
//=================================================================


namespace WindowsFormsApp1
{
    class Execute
    {
        public static void ExecuteCleaning(HashSet<string>[] path_list,string user_path, ProgressBar progressBar1,ToolStripStatusLabel sstrip, BackgroundWorker worker)
        {
            //Dans le dossier :  Working_WindowsFormsApp

            

            sstrip.Text = "(1/10)starting cleaning";
            Application.DoEvents();


            for (int i=0; i<path_list.Length; i++){
                path_list[i] = new HashSet<string>();
            }

            string zip_path = user_path + "/Extract.zip";
            Console.WriteLine("zip : " + zip_path);

            if ((File.Exists(zip_path)))
            {
                File.Delete(zip_path);
            }
            string extract_path = user_path + "/Extract";
            Console.WriteLine("extract folder : " + extract_path);

            if ((Directory.Exists(extract_path)))
            {
                CleanFolder(extract_path);
                Directory.Delete(extract_path);
            }

            var file = new FileInfo(user_path); // on se place dans le dossier dans lequel se trouve les données
            string path = file.FullName;
            if (!(file.Name == "DATA"))
            {
                if (Directory.Exists(path + "/DATA")){
                    path = path + "/DATA";
                }
                else
                {
                    throw new Exception("impossible de trouver un dossier DATA sous-jacent"); 
                }
            }

            if(file.Name == "DATA")
            {
                MessageBox.Show("Le dossier selectionné est nommé DATA. Cependant il devrait être nommé DATAs et doit contenir un dossier DATA. Veuillez selectionner un nouveau dossier valide.");
            }
            else
            {

                //=============================== CREATION DES DOSSIERS ============================================

                sstrip.Text = "(2/10) Creation des dossiers ephémères";
                Application.DoEvents();
                DirectoryManagement(path);
                worker.ReportProgress(1);

                //=============================== RECHERCHE DES FICHIERS LOG =======================================

                sstrip.Text = "(3/10) Recherche des fichiers log";
                Application.DoEvents();
                FindFiles(path, path_list, path+"/msg_nc1_data",path+"/DOC",progressBar1);
                worker.ReportProgress(1);

                //============================= DEFINITON DE LA PROGRESS BAR =======================================

                //sstrip.Text = "Definition de la progress bar";
                //Application.DoEvents();
                //DefinitonProgressBar(path_list, progressBar1);

                //=================================== PREMIER TRAITEMENT ===========================================

                sstrip.Text = "(4/10) Elimination des doublons";
                Application.DoEvents();
                DeleteAllDuplicatesFastWithMemoryManagement(path_list, path, progressBar1,worker);

                //=================================== CLEAR HASHSET ================================================
                //on clear tout les hashset car on les réutilise

                sstrip.Text = "(5/10) Clear hashset";
                Application.DoEvents();
                for (int i = 0; i < path_list.Length; i++)
                {
                    path_list[i].Clear();
                }
                worker.ReportProgress(1);

                //============================= ELIMINATION DOSSIER EPHEMERES ======================================

                sstrip.Text = "(6/10) Elimination dossiers ephémères";
                Application.DoEvents();
                Directory.Delete(path + "/old_data");
                worker.ReportProgress(1);

                //============================= CREATION ENVIRONNEMENT FINAL =======================================

                sstrip.Text = "(7/10) Création de l'environnement final";
                Application.DoEvents();
                DirectoryInfo final_path = Directory.GetParent(path);
                string destination_path = final_path.FullName + "/Extract";
                if (!(Directory.Exists(destination_path)))
                {
                    Directory.CreateDirectory(destination_path);
                }
                else
                {
                    DirectoryInfo di = new DirectoryInfo(destination_path);
                    di.Delete(true);
                    Directory.CreateDirectory(destination_path);
                }
                Directory.Move(path + "/new_data", destination_path + "/msg_h_data");
                Directory.Move(path + "/msg_nc1_data", destination_path + "/msg_nc1_data");
                Directory.Move(path + "/DOC", destination_path + "/DOC");

                //============================== RENOMMAGE DES FICHIER .LOG ========================================

                sstrip.Text = "(8/10) Renommage des fichiers log";
                Application.DoEvents();
                FinalRename(destination_path + "/msg_h_data");

                //=================================== ZIP DOSSIER ==================================================

                sstrip.Text = "(9/10)Zipping du dossier final";
                Application.DoEvents();
                CompressDirectory(destination_path);
                worker.ReportProgress(1);
                

                //=================================== RETOUR UTILISATEUR ===========================================

                sstrip.Text = "(10/10) Terminé !";
                Application.DoEvents();
                worker.ReportProgress(1);
                MessageBox.Show("Le dossier sans doublons se trouve à l'adresse : " + destination_path);
            }

        }

        /*
        Fonction qui permet l'organisation des fichiers.
        Le premier if permet d'inclure dans le dossier DOC les fichiers qui sont directement sous DATAs
        La boucle for traite deux evenement :
            - le dossier est intitulé NC1 : on ne trouve que des fichier nc1 donc a ne pas traiter
            - le dossier contient des fichiers .log : on ajoute le chemin du fichier dans path_list
        */
        private static void FindFiles(string path, HashSet<string>[] path_list, string msg_nc1_data, string doc_data, ProgressBar pBar1)
        {
            Console.WriteLine(path);
            var dir = new DirectoryInfo(path);
            FileSystemInfo[] arrayFiles = dir.GetFileSystemInfos();
            string[] tab = path.Split('/');
            FileSystemInfo data = new FileInfo(path);

            if (data.Name == "DATA")
            {
                var dir_doc = dir.Parent;
                FileSystemInfo[] doc = dir_doc.GetFileSystemInfos();
                foreach (FileSystemInfo info in doc)
                {
                    Console.WriteLine(info.ToString());
                    if(info.ToString() != "DATA")
                    {
                        File.Copy(dir_doc.FullName + "/" + info, doc_data + "/" + info);
                    }
                 }
            }

            foreach (FileSystemInfo info in arrayFiles)
             {

                FileAttributes attr = File.GetAttributes(path + "/" + info);

                if (info.ToString()=="NC1")
                {
                    
                    Console.WriteLine("In an NC1 directory");
                    var dirName = new DirectoryInfo(info.ToString());
                    CopyDirectory(dir.FullName + "/" + dirName.Name, msg_nc1_data,true);
                }

                else if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                    FindFiles(path + "/" + info, path_list, msg_nc1_data, doc_data,pBar1);
                }
    
                else{
                    var dirName = new DirectoryInfo(info.ToString());

                    char separator = '.';
                    string[] str = dirName.Name.Split(separator);
                    if (str[1] == "log")
                    { // on entre dans le cas ou le fichier est probe.log donc qui n'est pas du type probe.{int}.log
                        if(!(File.Exists(dir.FullName + "/probe.0.log")))
                        {
                            File.Move(dir.FullName + "/" + dirName.Name, dir.FullName + "/probe.0.log");
                        }
                    }
                    if (Int32.TryParse(str[1],out int chiffre)) {
                        Console.WriteLine("index : " + chiffre + " | taille du tableau : " + path_list.Length);
                        FileInfo[] tabInfo = dir.GetFiles("probe."+chiffre+".log");
                        if (tabInfo.Length > 0) {
                            path_list[chiffre].Add(dir.GetFiles("probe."+ chiffre +".log")[0].FullName);
                        }
                    }
                }
            }
        }

        /*
        La fonction PremierTraitement va parcourir la liste path_list regrouppant toute les chemin aux fichiers log 
        et va eliminer les doublons de deux fichiers .log d'un meme nombre en les concatenant grace à la fonction ElimDoublons.
        ( Le resultat va etre stocké dans un nouveau fichier créer dans le dossier éphémère new_data ).
        Input :  - path      : chemin parent
                 - path_list : Hashset contenant tout les chemins des fichier log triés en fonction de leurs nombre
                               fichier log du type : probe.{int}.log
                 - pBar1     : utile pour l'incrementation de la progressBar.
        */

        private static void PremierTraitement(string path, HashSet<string>[] path_list, ProgressBar pBar1)
        {

            for (int j = 0; j < path_list.Length; j++)
            {
                if (path_list[j] == null)
                {
                    break;
                }

                HashSet<string>.Enumerator em = path_list[j].GetEnumerator();
                int i = 0;
                string val1 = "";
                string val2 = "";

                while (em.MoveNext())
                {

                    string val = em.Current;

                    i++;
                    if (i % 2 == 0)
                    {
                        val2 = val;
                        ElimDoublons(val1, val2, path, j, i, 1,pBar1);
                    }
                    else
                    {
                        val1 = val;
                    }

                }
            }
        }

        /*
        La fonction clean_folder va simplement supprimer tout les elements d'un dossier. 
        Input : - destinationPath : chemin du dossier à nettoyer.
        */
        private static void CleanFolder(string destinationPath)
        {
            string[] files = Directory.GetFiles(destinationPath);
            foreach (string file in files)
            {
                File.Delete(file);
                Console.WriteLine($"{file} has been deleted.");
            }
            DirectoryInfo di = new DirectoryInfo(destinationPath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

        }

        /*
        La fonction move_from_oldFolder_to_newFolder2 permet de transferer le contenu d'un dossier vers un autre dossier. 
        Dans notre situation le dossier new_data vers old_data. 
        Input : - path : chemin du dossier contenant les dossier new _data et old_data.
        */

        private static string Move_from_oldFolder_to_newFolder2(string path)
        {
            string rootFolderPath = path + "/new_data";
            string destinationPath = path + "/old_data";
            string filesToDelete = "*.log";
            string[] fileList = Directory.GetFiles(rootFolderPath, filesToDelete);
            foreach (string file in fileList)
            {
                var fileInfo = new FileInfo(file);
                string fileToMove = rootFolderPath + "/" + fileInfo.Name;
                string moveTo = destinationPath + "/" + fileInfo.Name;
                if (!(File.Exists(moveTo)))
                {
                    File.Move(fileToMove, moveTo);
                }
            }
            return destinationPath;
        }

        /*
        La fonction SecondTraitement va effectuer la meme fonctionnalité que la fonction PremierTraitement. Cette fonction est recursive.
        Input :  - path      : chemin parent.
                 - path_list : Hashset contenant tout les chemins des fichier log triés en fonction de leurs nombre.
                               fichier log du type : probe.{int}.log
                 - iteration : indice de recursivité. Utile pour nommer les nouveaux fichiers.
                 - pBar1     : utile pour l'incrementation de la progressBar.     
        */
        private static void SecondTraitement(string path, HashSet<string>[] path_list, int iteration, ProgressBar pBar1)
        {

            //on deplace tout les fichiers de new_data dans old_data 
            string destinationPath = Move_from_oldFolder_to_newFolder2(path);

            var dir = new DirectoryInfo(path + "/old_data");
            FileSystemInfo[] arrayFiles = dir.GetFileSystemInfos();

            for (int i = 0; i < path_list.Length; i++)
            {
                path_list[i].Clear();
                path_list[i] = new HashSet<string>();
            }

            foreach (FileSystemInfo info in arrayFiles)
            {
                FileAttributes attr = File.GetAttributes(info.FullName);

                if (!((attr & FileAttributes.Directory) == FileAttributes.Directory))
                { // c'est un des fichiers .log que l'on a créé. 
                    char separator = '.' ;
                    string[] str = info.Name.Split(separator);
                    int chiffre = Int32.Parse(str[1]); // https://stackoverflow.com/questions/239103/convert-char-to-int-in-c-sharp
                    Console.Write("fichier : " + info.Name + " | nombre : " + chiffre + "\n");
                    path_list[chiffre].Add(dir.GetFiles(info.Name)[0].FullName);
                }

                else
                { // on se trouve dans un dossier donc ça ne peut pas etre un .log
                    //on ne fais rien
                }

            }
            for (int j = 0; j < path_list.Length; j++)
            {
                if (path_list[j] != null)
                {
                    HashSet<string>.Enumerator em = path_list[j].GetEnumerator();
                    int i = 0;
                    string val1 = "";
                    string val2 = "";

                    while (em.MoveNext())
                    {
                        string val = em.Current;
                        i++;

                        if (path_list[j].Count == 1)
                        {
                            var final_file = new FileInfo(em.Current);
                            File.Move(em.Current, path + "/msg_h_data/" + final_file.Name);
                        }

                        if (i % 2 == 0)
                        {
                            val2 = val;
                            ElimDoublons(val1, val2, path, j, i, iteration, pBar1);
                        }
                        else
                        {
                            val1 = val;
                        }
                    }
                }

            }
            //enfin on clean le folder old_path car les données ne sont plus utiles.
            CleanFolder(destinationPath);

        }

        /*
        La fonction ElimDoublons s'occupe de concatener deux fichiers en les transformant en liste, 
        eliminer les doublons de la liste resultante puis d'ecrire le résultat dans un nouveau fichier.
        Input : - path1 : chemin du premier fichier log.
                - path2 : chemin du second fichier log.
                - parent_path : chemin du dossier contenant le dossier new_data et old_data. (dans mon cas le dossier DATA).
                - i : entier de l'indice de récursivité ( correspond au nombre d'appel de la fonction second_traitement ). 
                - j : entier  correspondant au nombre de l'indice  du fichier .log ( probe.{int].log ).
                - z : entier qui prend le nombre du deuxième fichier log à traiter (utile pour attribuer un nom au nouveau fichier). 
                - pBar1 : progressBar à incrémenter.
        */

        private static void ElimDoublons(string path1, string path2, string parent_path, int j, int i, int z, ProgressBar pBar1)
        {
            //création des listes qui regrouppe toutes les lignes des fichiers désignés.
            var firstLogFile = File.ReadAllLines(path1);
            var firstlogList = new List<string>(firstLogFile);
            var secondLogFile = File.ReadAllLines(path2);
            var secondlogList = new List<string>(secondLogFile);

            Console.WriteLine("Concatenating : \n   > " + path1 + "\n   > " + path2);

            //On créé une liste de la concaténation des deux listes précédentes.

            List<string> resultLogFile = firstLogFile.Concat(secondLogFile).ToList();
           
            int a = resultLogFile.Count;
            //On élimine tout les doublons présents.
            resultLogFile = resultLogFile.Distinct().ToList();
            int b = a - resultLogFile.Count;
            string old_path = parent_path + "/old_data/probe." + j + "." + i + "." + z + ".log";
            string new_path = parent_path + "/new_data/probe." + j + "." + i + "." + z + ".log";
            //on écrit le résultat dans new_data
            var previousLines = new HashSet<string>();
            File.WriteAllLines(new_path, resultLogFile.Distinct().ToArray());
            FileInfo fInfo = new FileInfo(new_path);
            long size = fInfo.Length;

            Console.WriteLine("Doublons éliminés: " + b + " | taille finale du nouveau fichier : " + size*10e-7 + " Mo");
            Console.WriteLine("Nom du nouveau fichier : " + new_path);

            pBar1.PerformStep();
        }

        /*
        La fonction CopyDirectory permet de copier le contenu d'un dossier dans un autre dossier.
        Input : - sourceDir : chemin du dossier source.
                - destinationDir : chemin du dossier destination.
                - recursive : booléen qui lorsque assigné "true", copie la totalité du dossier ( càd avec le contenu des sous dossier)
        */ 
        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Sourcedirectory not found: {dir.FullName}");
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            Console.WriteLine(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                if (!(File.Exists(targetFilePath)))
                {
                    file.CopyTo(targetFilePath);
                }        
            }
            if(recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }


        /*
        La fonction FinalRename permet de renommer tout les fichiers log finaux dans une forme plus propre.
        Exemple:               FinalRename
                probe.2.5.14.log -----> probe.2.log
        Input : - destination_path : dossier contenant les fichier log à traiter. 

        */ 
        private static void FinalRename(string destination_path)
        {
            foreach (string fileInfo in Directory.GetFiles(destination_path))
            {
                FileInfo new_f = new FileInfo(fileInfo);
                char separator = '.';
                string[] str = new_f.Name.Split(separator);
                int number = Int32.Parse(str[1]); 
                if (!(File.Exists(new_f.DirectoryName + "/probe." + number + ".log")))
                {
                    File.Move(new_f.FullName, new_f.DirectoryName + "/probe." + number + ".log");
                }
            }
        }

        /*
        La fonction DefinitionProgressBar permet d'initialiser la progressBar (notamment  d'établir le
        seuil maximum. 
        Input : - path_list : Hashset contenant tout les chemins des fichier log triés en fonction de leurs nombre.
                              fichier log du type : probe.{int}.log
                - progressBar1 : progressBar à initialiser
        */
        internal static void DefinitonProgressBar(HashSet<string>[] path_list, ProgressBar progressBar1)
        {
            progressBar1.Visible = true;
            progressBar1.Minimum = 0;
            int total_size = 0;
            foreach (HashSet<string> i in path_list)
            {
                if (i.Count > 0)
                {
                    total_size += i.Count;
                }

            }
            total_size += 10;
            progressBar1.Maximum = total_size;
            progressBar1.Step = 1;
        }
        
        /*
        La fonction DirectoryManagement permet la création de tout les dossier utiles aux bons déroulement du programme.
        Deux dossier éphémères qui servent de receptacles temporaire pour les fichier log. [new_data et old_data]
        Trois dossier destiné à acceuillir les données finales. [DOC, msg_h_data, msg_nc1_data].
        Si les dossiers existent déjà, ils seront tous vidé à l'execution de cette fonction. Sinon les dossiers seront créé.
        Input : le chemin du dossier désigné par l'utilisateur.   
       
             * */
        private static void DirectoryManagement(string path)
        {

            string new_data_path = path + "/new_data";

            if (!(Directory.Exists(new_data_path)))
            {
                Directory.CreateDirectory(new_data_path);
            }
            else
            {
                CleanFolder(new_data_path);
            }


            string old_data_path = path + "/old_data";

            if (!(Directory.Exists(old_data_path)))
            {
                Directory.CreateDirectory(old_data_path);
            }
            else
            {
                CleanFolder(old_data_path);
            }

            string msg_h_path = path + "/msg_h_data";

            if (!(Directory.Exists(msg_h_path)))
            {
                Directory.CreateDirectory(msg_h_path);
            }
            else
            {
                CleanFolder(msg_h_path);
            }

            string msg_nc1_path = path + "/msg_nc1_data";

            if (!(Directory.Exists(msg_nc1_path)))
            {
                Directory.CreateDirectory(msg_nc1_path);
            }
            else
            {
                CleanFolder(msg_nc1_path);
            }

            string doc_path = path + "/DOC";

            if (!(Directory.Exists(doc_path)))
            {
                Directory.CreateDirectory(doc_path);
            }
            else
            {
                CleanFolder(doc_path);
            }
        }

        /*
        Cette fonction permet simplement de créer une version compressé d'un dossier.
        Input : le chemin du dossier à compresser.
        */
        public static void CompressDirectory(string fic)
        {
            string startPath = fic;
            FileInfo f = new FileInfo(fic);
            string zipPath = Path.ChangeExtension(fic, "zip"); 

            ZipFile.CreateFromDirectory(startPath, zipPath);
        }

        private static void DeleteAllDuplicatesFast(HashSet<string>[] path_list, string parent_path, ProgressBar pBar1, BackgroundWorker backgroundWorker1)
        {
            for (int j = 0; j < path_list.Length; j++)
            {
                if (path_list[j] == null)
                {
                    break;
                }
                Console.WriteLine(parent_path);
                HashSet<string>.Enumerator em = path_list[j].GetEnumerator();
                List<string> LogFile = new List<string>();
                while (em.MoveNext())
                {
                    
                    string val = em.Current;
                    Console.WriteLine(val);
                    var secondLogFile = File.ReadAllLines(val);
                    var secondlogList = new List<string>(secondLogFile);
                    try
                    {
                        LogFile = LogFile.Concat(secondLogFile).ToList();
                    }
                    catch (OutOfMemoryException)
                    {
                        LogFile = LogFile.Distinct().ToList();
                        LogFile = LogFile.Concat(secondLogFile).ToList();
                    }
                    //pBar1.PerformStep();
                    backgroundWorker1.ReportProgress(1);
                }
                LogFile = LogFile.Distinct().ToList();
                string new_path = parent_path + "/new_data/probe." + j + ".log";
                File.WriteAllLines(new_path, LogFile.Distinct().ToArray());
            }
        }

        private static void DeleteAllDuplicatesFastWithMemoryManagement(HashSet<string>[] path_list, string parent_path, ProgressBar pBar1, BackgroundWorker backgroundWorker1)
        {

            for (int j = 0; j < path_list.Length; j++)
            {
                if (path_list[j] == null)
                {
                    break;
                }
                HashSet<string>.Enumerator em = path_list[j].GetEnumerator();
                List<string> LogFile = new List<string>();
                while (em.MoveNext())
                {
                    var secondLogFile = File.ReadAllLines(em.Current);
                    LogFile = LogFile.Concat(secondLogFile).ToList();
                    LogFile = LogFile.Distinct().ToList();
                    backgroundWorker1.ReportProgress(1);
                }
                LogFile = LogFile.Distinct().ToList();
                string new_path = parent_path + "/new_data/probe." + j + ".log";
                File.WriteAllLines(new_path, LogFile.Distinct().ToArray());            
            }
        }
    }
}