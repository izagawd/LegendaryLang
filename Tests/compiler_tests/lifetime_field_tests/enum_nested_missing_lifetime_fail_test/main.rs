struct Ref['a] { val: &'a i32 }
enum Maybe {
    Some(Ref['a]),
    None
}
fn main() -> i32 { 0 }
