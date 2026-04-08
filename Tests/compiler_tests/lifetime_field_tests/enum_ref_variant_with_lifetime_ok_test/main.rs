enum Holder['a] {
    Some(&'a i32),
    None
}
fn main() -> i32 {
    let x: i32 = 99;
    let h = Holder.Some(&x);
    match h {
        Holder.Some(r) => *r,
        Holder.None => 0
    }
}
