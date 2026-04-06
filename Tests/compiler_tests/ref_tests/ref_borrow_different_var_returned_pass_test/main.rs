struct Holder['a] {
    r: &'a uniq i32
}

fn main() -> i32 {
    let a = 5;
    let result = 42;
    let h = make Holder { r: &uniq a };
    result
}
