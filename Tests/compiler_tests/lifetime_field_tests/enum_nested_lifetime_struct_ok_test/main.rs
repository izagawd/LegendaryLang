struct Ref['a] { val: &'a i32 }
enum Maybe['a] {
    Some(Ref['a]),
    None
}
fn main() -> i32 {
    let x: i32 = 88;
    let m = Maybe.Some(make Ref { val: &x });
    match m {
        Maybe.Some(r) => *r.val,
        Maybe.None => 0
    }
}
