struct Pair['a, 'b] {
    x: &'a mut i32,
    y: &'b mut i32
}

fn main() -> i32 {
    let a = 5;
    let b = 10;
    let result = 99;
    let p = make Pair { x: &mut a, y: &mut b };
    result
}
