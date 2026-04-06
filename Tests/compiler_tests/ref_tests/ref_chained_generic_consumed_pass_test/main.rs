struct Holder['a] {
    r: &'a uniq i32
}

fn Pass[T:! type](x: T) -> T { x }
fn DropIt[T:! type](x: T) {}

fn main() -> i32 {
    let a = 5;
    let h = make Holder { r: &uniq a };
    let h2 = Pass(Pass(h));
    DropIt(h2);
    a
}
