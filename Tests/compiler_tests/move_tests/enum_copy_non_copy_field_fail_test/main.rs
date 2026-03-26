struct G {}
enum Idk {
    A,
    C(G)
}
impl Copy for Idk {}
fn main() -> i32 {
    4
}
