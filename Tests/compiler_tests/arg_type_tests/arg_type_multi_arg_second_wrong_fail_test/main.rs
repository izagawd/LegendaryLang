struct Pair { a: i32, b: i32 }
impl Copy for Pair {}
fn combine(x: i32, p: Pair) -> i32 { x + p.a }
fn main() -> i32 {
    combine(5, 10)
}
