enum Opt {
    Some(i32),
    None
}
fn consume(o: Opt) -> i32 {
    match o {
        Opt.Some(v) => v,
        Opt.None => 0
    }
}
fn main() -> i32 {
    let x = Opt.Some(5);
    consume(x);
    consume(x)
}
