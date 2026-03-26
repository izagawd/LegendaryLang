fn main() -> i32 {
    let gotten = {
        let inner = {
            let a = 5;
            &a
        };
        *inner
    };
    gotten
}
