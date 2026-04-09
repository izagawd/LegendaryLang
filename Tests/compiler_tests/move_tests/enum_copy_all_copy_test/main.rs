struct G {
    val: i32
}
impl Copy for G {}
enum Idk {
    A,
    C(G)
}
impl Copy for Idk {}
fn main() -> i32 {
    let x = Idk.C(make G { val : 5 });
    let y = x;
    let z = x;
    4
}
