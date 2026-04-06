struct Pair(A:! type, B:! type) {
    first: A,
    second: B
}

fn main() -> i32 {
    let p = make Pair { first : 10, second : 20 };
    p.first + p.second
}
