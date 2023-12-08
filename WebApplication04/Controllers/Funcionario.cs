using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using WebApplication4.Models.Contexts;
using WebApplication4.Models.Entities;
using WebApplication4.ViewModels.Paciente;

namespace WebApplication4.Controllers
{
    public class FuncionarioController : Controller
    {
        private readonly SisMedContext _context;
        private readonly IValidator<AdicionarPacienteViewModel> _adicionarPacienteValidator;
        private readonly IValidator<EditarPacienteViewModel> _editarPacienteValidator;
        private const int TAMANHO_PAGINA = 10;
        public FuncionarioController(SisMedContext context, IValidator<AdicionarPacienteViewModel> adicionarPacienteValidator, IValidator<EditarPacienteViewModel> editarPacienteValidator)
        {
            _context = context;
            _adicionarPacienteValidator = adicionarPacienteValidator;
            _editarPacienteValidator = editarPacienteValidator;
        }

        // GET: FuncionarioController
        public ActionResult Index(string filtro, int pagina = 1)
        {
            ViewBag.Filtro = filtro;

            var condicao = (Paciente p) => String.IsNullOrWhiteSpace(filtro) || p.Nome.ToUpper().Contains(filtro.ToUpper()) || p.CPF.Contains(filtro.Replace(".", "").Replace("-", ""));

            var pacientes = _context.Pacientes.Where(condicao)
                                              .Select(p => new ListarPacienteViewModel
                                              {
                                                  Id = p.Id,
                                                  Nome = p.Nome,
                                                  CPF = p.CPF
                                              });
                                              

            ViewBag.NumeroPagina = pagina;
            ViewBag.TotalPaginas = Math.Ceiling((decimal)pacientes.Count() / TAMANHO_PAGINA);
            return View(pacientes.Skip((pagina - 1) * TAMANHO_PAGINA)
                                 .Take(TAMANHO_PAGINA)
                                 .ToList());
        }

        // GET: FuncionarioController/Create
        public ActionResult Adicionar()
        {
            return View();
        }

        // POST: FuncionarioController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Adicionar(AdicionarPacienteViewModel dados)
        {
            var validacao = _adicionarPacienteValidator.Validate(dados);

            if (!validacao.IsValid)
            {
                validacao.AddToModelState(ModelState, "");
                return View(dados);
            }

            var funcionario = new Paciente
            {
                CPF = Regex.Replace(dados.CPF, "[^0-9]", ""),
                Nome = dados.Nome,
                DataNascimento = dados.DataNascimento
            };

            _context.Pacientes.Add(funcionario);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // GET: FuncionarioController/Details/5
        public ActionResult Editar(int id)
        {
            var funcionario = _context.Pacientes.Find(id);
                                             
            if (funcionario != null)
            {
                var informacoesComplementares = _context.InformacoesComplementaresPaciente.FirstOrDefault(i => i.IdPaciente == id);

                return View(new EditarPacienteViewModel
                {
                    Id = funcionario.Id,
                    CPF = funcionario.CPF,
                    Nome = funcionario.Nome,
                    DataNascimento = funcionario.DataNascimento,
                    Alergias = informacoesComplementares?.Alergias,
                    MedicamentosEmUso = informacoesComplementares?.MedicamentosEmUso,
                    CirurgiasRealizadas = informacoesComplementares?.CirurgiasRealizadas
                });''
            }

            return NotFound();
        }

        // POST: FuncionarioController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(int id, EditarPacienteViewModel dados)
        {
            var validacao = _editarPacienteValidator.Validate(dados);

            if (!validacao.IsValid)
            {
                validacao.AddToModelState(ModelState, "");
                return View(dados);
            }

            var funcionario = _context.Pacientes.Find(id);

            if (funcionario != null)
            {
                funcionario.CPF = Regex.Replace(dados.CPF, "[^0-9]", "");
                funcionario.Nome = dados.Nome;
                funcionario.DataNascimento = dados.DataNascimento;

                var informacoesComplementares = _context.InformacoesComplementaresPaciente.FirstOrDefault(i => i.IdPaciente == id);

                if (informacoesComplementares == null)
                    informacoesComplementares = new InformacoesComplementaresPaciente();

                informacoesComplementares.Alergias = dados.Alergias;
                informacoesComplementares.MedicamentosEmUso = dados.MedicamentosEmUso;
                informacoesComplementares.CirurgiasRealizadas = dados.CirurgiasRealizadas;
                informacoesComplementares.IdPaciente = id;

                if (informacoesComplementares.Id > 0)
                    _context.InformacoesComplementaresPaciente.Update(informacoesComplementares);
                else
                    _context.InformacoesComplementaresPaciente.Add(informacoesComplementares);

                _context.Pacientes.Update(funcionario);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }
            
            return NotFound();
        }

        // GET: FuncionarioController/Excluir/5
        public ActionResult Excluir(int id)
        {
            var funcionario = _context.Pacientes.Find(id);

            if (funcionario != null)
            {
                return View(new EditarPacienteViewModel
                {
                    Id = funcionario.Id,
                    CPF = funcionario.CPF,
                    Nome = funcionario.Nome,
                    DataNascimento = funcionario.DataNascimento
                });
            }
            
            return NotFound();
        }

        // POST: FuncionarioController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Excluir(int id, IFormCollection collection)
        {
            var funcionario = _context.Pacientes.Find(id);

            if (funcionario != null)
            {
                var informacoesComplementares = _context.InformacoesComplementaresPaciente.FirstOrDefault(i => i.IdPaciente == id);

                if (informacoesComplementares != null)
                {
                    _context.InformacoesComplementaresPaciente.Remove(informacoesComplementares);
                }

                _context.Pacientes.Remove(funcionario);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }
            
            return NotFound();
        }

        public ActionResult Buscar(string filtro)
        {
            var pacientes = new List<ListarPacienteViewModel>();

            if (!String.IsNullOrWhiteSpace(filtro))
            {
                pacientes = _context.Pacientes.Where(p => p.Nome.Contains(filtro) || p.CPF.Contains(filtro.Replace(".", "").Replace("-", "")))
                                              .Take(10)
                                              .Select(p => new ListarPacienteViewModel
                                              {
                                                  Id = p.Id,
                                                  Nome = p.Nome,
                                                  CPF = p.CPF
                                              })
                                              .ToList();
            }

            return Json(pacientes);
        }
    }
}
