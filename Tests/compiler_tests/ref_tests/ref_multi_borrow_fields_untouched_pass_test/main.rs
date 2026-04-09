struct Pair['a, 'b] {
    x: &'a uniq i32,
    y: &'b uniq i32
}

fn main() -> i32 {
    let a = 5;
    let b = 10;
    let result = 99;
    let p = make Pair { x: &uniq a, y: &uniq b };
    result
}
