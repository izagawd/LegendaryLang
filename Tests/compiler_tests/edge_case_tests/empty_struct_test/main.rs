struct Unit {}
impl Copy for Unit {}
fn takes_unit(u: Unit) -> i32 { 42 }
fn main() -> i32 {
    let u = Unit {};
    takes_unit(u)
}
