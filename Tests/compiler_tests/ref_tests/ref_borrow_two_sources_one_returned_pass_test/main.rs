struct Holder['a] {
    r: &'a uniq i32
}

fn main() -> i32 {
    let a = 5;
    let b = 10;
    let c = 20;
    let h1 = make Holder { r: &uniq a };
    let h2 = make Holder { r: &uniq b };
    c
}
