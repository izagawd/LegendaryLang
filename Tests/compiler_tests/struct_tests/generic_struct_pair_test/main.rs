struct Pair(A:! Sized, B:! Sized) {
    first: A,
    second: B
}

fn main() -> i32 {
    let p = make Pair(i32, i32) { first : 10, second : 20 };
    p.first + p.second
}
