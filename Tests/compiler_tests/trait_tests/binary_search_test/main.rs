fn find_sqrt_floor(n: i32) -> i32 {
    let low = 0;
    let high = n;
    let result = 0;
    while low <= high {
        let mid = (low + high) / 2;
        if mid * mid <= n {
            result = mid;
            low = mid + 1;
        } else {
            high = mid - 1;
        };
    };
    result
}

fn main() -> i32 {
    find_sqrt_floor(100)
}
